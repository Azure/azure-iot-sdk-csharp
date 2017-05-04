using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;

namespace ConsoleSample
{
    class Program
    {
        const string connectionString = "<replace_with_iothub_connection_string>";
        const string deviceID = "new_device";
        static void Main(string[] args)
        {
            AddDevice().Wait();
            //SendMessage().Wait();
            //RemoveDevice().Wait();
        }

        static async Task SendMessage()
        {
            var serviceClient = ServiceClient.CreateFromConnectionString(connectionString, TransportType.Amqp);
            var str = "Hello, Cloud!";
            var message = new Message(Encoding.ASCII.GetBytes(str));
            await serviceClient.SendAsync(deviceID, message);
            Console.WriteLine("C2D Message Sent");
        }

        static async Task AddDevice()
        {
            RegistryManager manager = RegistryManager.CreateFromConnectionString(connectionString);
            await manager.AddDeviceAsync(new Device(deviceID));
            Console.WriteLine("Device Added");
        }

        static async Task RemoveDevice()
        {
            RegistryManager manager = RegistryManager.CreateFromConnectionString(connectionString);
            await manager.RemoveDeviceAsync(deviceID);
            Console.WriteLine("Device Removed");
        }
    }
}
