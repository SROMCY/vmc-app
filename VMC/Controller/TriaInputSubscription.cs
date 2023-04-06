using System;
using System.Collections.Specialized;
using Triamec.Tam;
using Triamec.Tam.Registers;
using Triamec.Tam.Subscriptions;
using Triamec.TriaLink;

namespace VMC.Controller
{
    public class TriaInputSubscription : IDisposable
    {
        public event EventHandler<RegisterChangedEventArgs> RegisterChanged;

        private IClientSubscription listener;
        private int packetValueIndex;
        private int bits;
        public TriaInputSubscription(ITamReadonlyRegister<int> register)
        {
            bits = int.MinValue;
            SetupListener(register);
        }

        /// <summary>
        /// Creates the listener subscription.
        /// </summary>
        /// <exception cref="SubscriptionException">Could not set the listener down.</exception>
        private void SetupListener(ITamReadonlyRegister<int> register)
        {
            if (listener == null)
            {
                TamLink link = register.Device.Station.Link;
                Publisher publisher = new Publisher(500, register); // let the inputs register be published at a low rate of 10000 Hz / 500 = 20 Hz
                packetValueIndex = publisher.GetValueIndex(register);
                listener = link.SubscriptionManager.SubscribeEvent(publisher); // create the subsription                
                listener.PacketSender.PacketsAvailable += OnPacketsAvailable; // subscribe to the data stream
                listener.Enable(); // Enable the subscription
            }
        }

        /// <summary>
        /// Dissolves the listener subscription.
        /// </summary>
        /// <exception cref="SubscriptionException">Could not tear the listener down.</exception>
        public void Dispose()
        {
            if (listener != null)
            {
                listener.Disable(); // switch data transmission off
                listener.Unsubscribe(); // unsubscribe from the device
                listener.Dispose(); // unsubscribe from the manager
            }
        }

        /// <summary>
        /// Called when new data from the listener subscription gets available. Updates the state propperties as appropriate.
        /// </summary>
        private void OnPacketsAvailable(object sender, EventArgs e)
        {
            // Get all available raw packets.
            // The values are transported as raw TamValue32 structures
            // allowing to interpret them AsInt32, AsSingle or AsBoolean.
            TamValue32[][] packets = listener.PacketSender.Dequeue();

            if (packets.Length > 0)
            {
                TamValue32[] packet = packets[packets.Length - 1]; // only consider most recent packet in case of multiple packets
                int input = packet[packetValueIndex]; // get the register value from the packet, using the previously cached index

                if (input != bits)
                {
                    bits = input;
                    RegisterChanged.Invoke(this, new RegisterChangedEventArgs(new BitVector32(input)));
                }
            }
        }
    }

    public class RegisterChangedEventArgs : EventArgs
    {
        private BitVector32 bits;

        public RegisterChangedEventArgs(BitVector32 inputBits)
        {
            bits = inputBits;
        }

        public bool GetBit(int mask)
        {
            return bits[mask];
        }
    }
}
