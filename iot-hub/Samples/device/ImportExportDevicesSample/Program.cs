using System;
using System.Threading.Tasks;

namespace ImportExportDevices
{
    public class Program
    {

        public static void Main(string[] args)
        {
            //To use this sample, uncomment the bits you want to see run.
            //It's probably not optimal to create a bunch of devices and then delete them right away.
            //As for the # of devices to test with, the size of the hub you are using should
            //  correspond to the number of devices you want to create and test with.
            //  For example, if you want to create a million devices, don't use a hub with a Basic sku.
            
            //Console.WriteLine("Add devices to the hub.");
            // Add devices to the hub; specify how many.
            IOTHubDevices.AddDevicesToHub(1000).Wait();

            //Console.WriteLine("Read devices from the hub, write to blob storage.");
            // Read the list of registered devices for the IoT Hub.
            // Write them to blob storage.
            IOTHubDevices.ExportDevicesToBlobStorage().Wait();

            //Console.WriteLine("Export device list to blob storage, then read them in and display them.");
            //IOTHubDevices.ReadAndDisplayExportedDeviceList().Wait();

            //Console.WriteLine("Delete all devices from the hub.");
            //IOTHubDevices.DeleteAllDevicesFromHub().Wait();

            Console.WriteLine("Finished.");
            Console.WriteLine();
            Console.Write("Press any key to continue.");
            Console.ReadLine();
        }


    }
}
