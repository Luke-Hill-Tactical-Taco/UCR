﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using UCR.Core.Models.Binding;
using UCR.Core.Models.Device;

namespace UCR.Core.Models.Plugin
{
    public abstract class Plugin
    {
        // Persistence
        public string Title { get; set; }
        public List<DeviceBinding> Inputs { get; }
        public List<DeviceBinding> Outputs { get; }

        // Runtime
        public delegate void PluginBindingChanged(Plugin plugin);
        internal Profile.Profile ParentProfile { get; set; }
        [XmlIgnore]
        public PluginBindingChanged BindingCallback { get; set; }

        // Abstract
        public abstract string PluginName();
        
        protected Plugin()
        {
            Inputs = new List<DeviceBinding>();
            Outputs = new List<DeviceBinding>();
        }

        public bool Activate(Context context)
        {
            var success = true;
            success &= SubscribeInputs(context);
            return success;
        }

        public Device.Device GetDevice(DeviceBinding deviceBinding)
        {
            return ParentProfile.GetDevice(deviceBinding);
        }

        protected void WriteOutput(DeviceBinding output, long value)
        {
            if (output?.DeviceType == null) return;
            var device = ParentProfile.GetLocalDevice(output);
            device.WriteOutput(ParentProfile.context, output, value);
        }

        public virtual List<DeviceBinding> GetInputs()
        {
            return Inputs.Select(input => new DeviceBinding(input)).ToList();
        }

        private bool SubscribeInputs(Context context)
        {
            var success = true;
            foreach (var input in GetInputs())
            {
                var device = context.ActiveProfile.GetLocalDevice(input);
                if (device != null)
                {
                    success &= device.AddDeviceBinding(input);
                }
                else
                {
                    success = false;
                }
            }
            return success;
        }

        protected DeviceBinding InitializeInputMapping(DeviceBinding.ValueChanged callbackFunc)
        {
            return InitializeMapping(DeviceIoType.Input, callbackFunc);
        }

        protected DeviceBinding InitializeOutputMapping()
        {
            return InitializeMapping(DeviceIoType.Output, null);
        }

        private DeviceBinding InitializeMapping(DeviceIoType deviceIoType, DeviceBinding.ValueChanged callbackFunc)
        {
            var deviceBinding = new DeviceBinding(callbackFunc, this, deviceIoType);
            switch(deviceIoType)
            {
                case DeviceIoType.Input:
                    Inputs.Add(deviceBinding);
                    break;
                case DeviceIoType.Output:
                    Outputs.Add(deviceBinding);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(deviceIoType), deviceIoType, null);
            }
            return deviceBinding;
        }

        public List<Device.Device> GetDeviceList(DeviceBinding deviceBinding)
        {
            return ParentProfile.GetDeviceList(deviceBinding);
        }

        public void Rename(string title)
        {
            Title = title;
            ParentProfile.context.ContextChanged();
        }

        internal void PostLoad(Context context, Profile.Profile profile)
        {
            ParentProfile = profile;
            BindingCallback = profile.OnDeviceBindingChange;

            ZipDeviceBindingList(Inputs);
            ZipDeviceBindingList(Outputs);
        }

        private static void ZipDeviceBindingList(IList<DeviceBinding> deviceBindings)
        {
            if (deviceBindings.Count == 0) return;
            var split = deviceBindings.Count / 2;
            for (var i = 0; i < split; i++)
            {
                deviceBindings[i].IsBound = deviceBindings[i + split].IsBound;
                deviceBindings[i].DeviceType= deviceBindings[i + split].DeviceType;
                deviceBindings[i].DeviceNumber = deviceBindings[i + split].DeviceNumber;
                deviceBindings[i].KeyType = deviceBindings[i + split].KeyType;
                deviceBindings[i].KeyValue = deviceBindings[i + split].KeyValue;
                deviceBindings[i].KeySubValue = deviceBindings[i + split].KeySubValue;
            }

            for (var i = deviceBindings.Count - 1; i >= split ; i--)
            {
                deviceBindings.Remove(deviceBindings[i]);
            }
        }
    }
}
