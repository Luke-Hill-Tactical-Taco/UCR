﻿using System.Collections.Generic;
using UCR.Core.Models.Binding;
using UCR.Core.Models.Device;

namespace UCR.Core.Managers
{
    public class DevicesManager
    {
        private readonly Context _context;

        public DevicesManager(Context context)
        {
            _context = context;
        }

        /// <summary>
        /// Gets a list of available devices from the backend
        /// </summary>
        /// <param name="type"></param>
        public List<DeviceGroup> GetAvailableDeviceList(DeviceIoType type)
        {
            var deviceGroupList = new List<DeviceGroup>();
            var providerList = type == DeviceIoType.Input
                ? _context.IOController.GetInputList()
                : _context.IOController.GetOutputList();

            foreach (var providerReport in providerList)
            {
                var deviceGroup = new DeviceGroup(providerReport.Key);
                foreach (var ioWrapperDevice in providerReport.Value.Devices)
                {
                    deviceGroup.Devices.Add(new Device(ioWrapperDevice.Value, providerReport.Value, type));
                }
                deviceGroupList.Add(deviceGroup);
            }
            return deviceGroupList;
        }
    }
}