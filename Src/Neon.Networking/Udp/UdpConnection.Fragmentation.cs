using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Neon.Networking.Messages;
using Neon.Networking.Udp.Channels;
using Neon.Networking.Udp.Messages;

namespace Neon.Networking.Udp
{
    public partial class UdpConnection
    {
        ConcurrentDictionary<ushort, FragmentHolder> fragments;
        ushort fragmentationGroupOut;
        object fragmentationGroupMutex = new object();


        ushort GetNextFragementationGroupId()
        {
            ushort result = 0;
            lock (fragmentationGroupMutex)
                result = fragmentationGroupOut++;
            return result;
        }


        void ManageFragment(Datagram datagram)
        {
            if (!datagram.IsFragmented)
            {
                throw new InvalidOperationException("Datagram isn't fragemented");
            }

            if (datagram.FragmentationInfo.Frame > datagram.FragmentationInfo.Frames)
            {
                logger.Error($"#{Id} got invalid fragmented datagram. Frame {datagram.FragmentationInfo.Frame} > frames {datagram.FragmentationInfo.Frames}. Dropping...");
                return;
            }

            FragmentHolder fragmentHolder = this.fragments.GetOrAdd(datagram.FragmentationInfo.FragmentationGroupId, (groupId) =>
            {
                return new FragmentHolder(datagram.FragmentationInfo.FragmentationGroupId, datagram.FragmentationInfo.Frames);
            });

            fragmentHolder.SetFrame(datagram);

            if (fragmentHolder.IsCompleted)
            {
                logger.Trace($"#{Id} fragment {datagram.FragmentationInfo.FragmentationGroupId} completed, merging...");
                if (this.fragments.TryRemove(datagram.FragmentationInfo.FragmentationGroupId, out FragmentHolder removed))
                {
                    var message = fragmentHolder.Merge(this.peer);
                    ReleaseMessage(message);
                    fragmentHolder.Dispose();
                }
            }
        }


        bool CheckCanBeSendUnfragmented(RawMessage message)
        {
            if (message.Length + Datagram.GetHeaderSize(false) > Mtu)
                return false;
            return true;
        }

        async Task SendFragmentedMessage(RawMessage message, IChannel channel)
        {
            message.Position = 0;
            int mtu = this.Mtu;
            ushort frame = 0;
            ushort frames = (ushort)Math.Ceiling(message.Length / (float)(mtu - Datagram.GetHeaderSize(true)));
            ushort groupId = GetNextFragementationGroupId();
            List<Task> tasks = new List<Task>(frames);
            do
            {
                Debug.Assert(frame < frames, "frame >= frames");

                Datagram datagramFrag = peer.CreateDatagram(mtu, channel.Descriptor);
                datagramFrag.Type = MessageType.UserData;
                datagramFrag.SetFragmentation(new Datagram.FragmentInfo(groupId, frame, frames));
                datagramFrag.Compressed = message.Compressed;
                datagramFrag.Encrypted = message.Encrypted;

                int toCopy = mtu - Datagram.GetHeaderSize(true);
                if (toCopy > message.Length - message.Position)
                {
                    toCopy = message.Length - message.Position;
                }
                message.CopyTo(datagramFrag, toCopy);
                
                tasks.Add(channel.SendDatagramAsync(datagramFrag));
                frame++;

            } while (message.Position < message.Length);
            
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }


    }
}
