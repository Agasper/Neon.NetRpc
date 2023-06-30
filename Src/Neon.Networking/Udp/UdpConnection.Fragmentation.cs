using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Neon.Networking.Messages;
using Neon.Networking.Udp.Channels;
using Neon.Networking.Udp.Messages;

namespace Neon.Networking.Udp
{
    public partial class UdpConnection
    {
        readonly object _fragmentationGroupMutex = new object();
        readonly ConcurrentDictionary<ushort, FragmentHolder> _fragments;
        ushort _fragmentationGroupOut;


        ushort GetNextFragementationGroupId()
        {
            ushort result = 0;
            lock (_fragmentationGroupMutex)
            {
                result = _fragmentationGroupOut++;
            }

            return result;
        }


        void ManageFragment(Datagram datagram)
        {
            if (!datagram.IsFragmented) throw new InvalidOperationException("Datagram isn't fragemented");

            if (datagram.FragmentationInfo.Frame > datagram.FragmentationInfo.Frames)
            {
                _logger.Error(
                    $"#{Id} got invalid fragmented datagram. Frame {datagram.FragmentationInfo.Frame} > frames {datagram.FragmentationInfo.Frames}. Dropping...");
                return;
            }

            FragmentHolder fragmentHolder = _fragments.GetOrAdd(datagram.FragmentationInfo.FragmentationGroupId,
                groupId =>
                {
                    return new FragmentHolder(datagram.FragmentationInfo.FragmentationGroupId,
                        datagram.FragmentationInfo.Frames);
                });

            fragmentHolder.SetFrame(datagram);

            if (fragmentHolder.IsCompleted)
            {
                _logger.Trace(
                    $"#{Id} fragment {datagram.FragmentationInfo.FragmentationGroupId} completed, merging...");
                if (_fragments.TryRemove(datagram.FragmentationInfo.FragmentationGroupId, out FragmentHolder removed))
                {
                    UdpMessageInfo messageInfo = fragmentHolder.Merge(Parent);
                    ReleaseMessage(messageInfo);
                    fragmentHolder.Dispose();
                }
            }
        }


        bool CheckCanBeSendUnfragmented(IRawMessage message)
        {
            if (message.Length + Datagram.GetHeaderSize(false) > Mtu)
                return false;
            return true;
        }

        async Task SendFragmentedMessage(IRawMessage message, IChannel channel, CancellationToken cancellationToken)
        {
            message.Position = 0;
            int mtu = Mtu;
            ushort frame = 0;
            var frames = (ushort) Math.Ceiling(message.Length / (float) (mtu - Datagram.GetHeaderSize(true)));
            ushort groupId = GetNextFragementationGroupId();
            var tasks = new List<Task>(frames);
            do
            {
                Debug.Assert(frame < frames, "frame >= frames");

                Datagram datagramFrag = Parent.CreateDatagram(mtu, channel.Descriptor);
                datagramFrag.Type = MessageType.UserData;
                datagramFrag.SetFragmentation(new Datagram.FragmentInfo(groupId, frame, frames));
                datagramFrag.Compressed = message.Compressed;
                datagramFrag.Encrypted = message.Encrypted;

                int toCopy = mtu - Datagram.GetHeaderSize(true);
                if (toCopy > message.Length - message.Position) toCopy = message.Length - message.Position;
                message.CopyTo(datagramFrag, toCopy);

                tasks.Add(channel.SendDatagramAsync(datagramFrag,cancellationToken));
                frame++;
            } while (message.Position < message.Length);

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
    }
}