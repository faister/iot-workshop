namespace iotedgeblemodule
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using System.Runtime.Loader;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Net;
    using System.Net.Sockets;
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Devices.Client.Transport.Mqtt;
    using Microsoft.Azure.Devices.Shared;
    using Newtonsoft.Json;
    using StackExchange.Redis;

    class Program
    {
        static string redisIPAddress { get; set; } = "172.18.0.1";
        static string redisChannelName { get; set; } = "data";

        static DeviceClient ioTHubModuleClient;

        static ConnectionMultiplexer redis;
        static ISubscriber sub;

        static int counter;

        static void Main(string[] args)
        {
            // The Edge runtime gives us the connection string we need -- it is injected as an environment variable
            string connectionString = Environment.GetEnvironmentVariable("EdgeHubConnectionString");

            // Cert verification is not yet fully functional when using Windows OS for the container
            bool bypassCertVerification = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            if (!bypassCertVerification) InstallCert();
            Init(connectionString, bypassCertVerification).Wait();

            Subscribe(redisIPAddress, redisChannelName, ioTHubModuleClient);
        
            // Wait until the app unloads or is cancelled
            var cts = new CancellationTokenSource();
            AssemblyLoadContext.Default.Unloading += (ctx) => cts.Cancel();
            Console.CancelKeyPress += (sender, cpe) => cts.Cancel();
            WhenCancelled(cts.Token).Wait();
            
        }

        /// <summary>
        /// Handles cleanup operations when app is cancelled or unloads
        /// </summary>
        
        public static void Subscribe(string redisIPAddress, string redisChannelName, object ioTHubModuleClient)
        {
            try
            {
                Console.WriteLine("Subscribing to {0}:{1}", redisIPAddress, redisChannelName);
                
                redis = ConnectionMultiplexer.Connect(redisIPAddress + ",abortConnect=false");
                sub = redis.GetSubscriber();
                sub.Subscribe(redisChannelName, (channel, message) => {
                    PipeMessage(new Message(Encoding.UTF8.GetBytes(message.ToString())), ioTHubModuleClient);
                    Console.WriteLine("{0}: {1}", (string)channel, (string)message);
                });

                Console.WriteLine("Finished subscribing to {0}:{1}", redisIPAddress, redisChannelName);
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception {0}: ", e.Message);
            }
        }

        public static void UnsubscribeFromAll()
        {
            try
            {
                Console.WriteLine("Unsubscribing from all channels");
                redis.GetSubscriber().UnsubscribeAll();
                redis.Dispose();
                Console.WriteLine("Finished unsubscribing from all channels");
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception {0}: ", e.Message);
            }
        }
        
        public static Task WhenCancelled(CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();
            cancellationToken.Register(s => ((TaskCompletionSource<bool>)s).SetResult(true), tcs);
            return tcs.Task;
        }

        /// <summary>
        /// Add certificate in local cert store for use by client for secure connection to IoT Edge runtime
        /// </summary>
        static void InstallCert()
        {
            string certPath = Environment.GetEnvironmentVariable("EdgeModuleCACertificateFile");
            if (string.IsNullOrWhiteSpace(certPath))
            {
                // We cannot proceed further without a proper cert file
                Console.WriteLine($"Missing path to certificate collection file: {certPath}");
                throw new InvalidOperationException("Missing path to certificate file.");
            }
            else if (!File.Exists(certPath))
            {
                // We cannot proceed further without a proper cert file
                Console.WriteLine($"Missing path to certificate collection file: {certPath}");
                throw new InvalidOperationException("Missing certificate file.");
            }
            X509Store store = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadWrite);
            store.Add(new X509Certificate2(X509Certificate2.CreateFromCertFile(certPath)));
            Console.WriteLine("Added Cert: " + certPath);
            store.Close();
        }


        /// <summary>
        /// Initializes the DeviceClient and sets up the callback to receive
        /// messages containing temperature information
        /// </summary>
        static async Task Init(string connectionString, bool bypassCertVerification = false)
        {
            Console.WriteLine("Connection String {0}", connectionString);

            MqttTransportSettings mqttSetting = new MqttTransportSettings(TransportType.Mqtt_Tcp_Only);
            // During dev you might want to bypass the cert verification. It is highly recommended to verify certs systematically in production
            if (bypassCertVerification)
            {
                mqttSetting.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
            }
            ITransportSettings[] settings = { mqttSetting };

            // Open a connection to the Edge runtime
            ioTHubModuleClient = DeviceClient.CreateFromConnectionString(connectionString, settings);
            await ioTHubModuleClient.OpenAsync();
            Console.WriteLine("IoT Hub module client initialized.");

            await ioTHubModuleClient.SetDesiredPropertyUpdateCallbackAsync(onDesiredPropertiesUpdate, ioTHubModuleClient);
            
            Console.WriteLine("Init Complete");
        }

        static Task onDesiredPropertiesUpdate(TwinCollection desiredProperties, object ioTHubModuleClient)
        {
            try
            {
                Console.WriteLine("Desired property change:");
                Console.WriteLine(JsonConvert.SerializeObject(desiredProperties));

                if (desiredProperties["redisIPAddress"] != null)
                    redisIPAddress = desiredProperties["redisIPAddress"];

                if (desiredProperties["redisChannelName"] != null)
                    redisChannelName = desiredProperties["redisChannelName"];
                
                Console.WriteLine("Unsubscribing...");
                UnsubscribeFromAll();
                Console.WriteLine("Resubscribing...");
                Subscribe(redisIPAddress, redisChannelName, ioTHubModuleClient);
            }
            catch (AggregateException ex)
            {
                foreach (Exception exception in ex.InnerExceptions)
                {
                    Console.WriteLine();
                    Console.WriteLine("Error when receiving desired property: {0}", exception);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("Error when receiving desired property: {0}", ex.Message);
            }
            Console.WriteLine("onDesiredPropertiesUpdate Complete");
            return Task.CompletedTask;
        }

        

        /// <summary>
        /// This method is called whenever the module is sent a message from the EdgeHub. 
        /// It just pipe the messages without any change.
        /// It prints all the incoming messages.
        /// </summary>
        static void PipeMessage(Message message, object userContext)
        {
            int counterValue = Interlocked.Increment(ref counter);

            var deviceClient = userContext as DeviceClient;
            if (deviceClient == null)
            {
                throw new InvalidOperationException("UserContext doesn't contain " + "expected values");
            }

            byte[] messageBytes = message.GetBytes();
            string messageString = Encoding.UTF8.GetString(messageBytes);
            Console.WriteLine($"Received message: {counterValue}, Body: [{messageString}]");

            if (!string.IsNullOrEmpty(messageString))
            {
                var pipeMessage = new Message(messageBytes);
                foreach (var prop in message.Properties)
                {
                    pipeMessage.Properties.Add(prop.Key, prop.Value);
                }
                deviceClient.SendEventAsync("output1", pipeMessage);
                Console.WriteLine("Received message sent");
            }
        }
    }
}
