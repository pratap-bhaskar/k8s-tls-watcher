using System;
using Prometheus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System.IO;
using k8s;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace KubernetesTLSWatcher
{
    class Program
    {
        public static IConfigurationRoot configuration;
        private static void ConfigureServices(IServiceCollection serviceCollection)
        {
            configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetParent(AppContext.BaseDirectory).FullName)
                .AddJsonFile("appsettings.json", false)
                .AddEnvironmentVariables()
                .Build();

            serviceCollection.AddSingleton<IConfigurationRoot>(configuration);
        }
        static void Main(string[] args)
        {
            ServiceCollection serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            
            SetupRequirements();
            try
            {
                MainAsync(args).GetAwaiter().GetResult();
            }
            catch (System.Exception ex)
            {
                Console.WriteLine($"Fatal error while occurred : {ex.Message}");
                Console.WriteLine(ex);
            }
        }

        private static async Task MainAsync(string[] args)
        {
            int secondsToSleep = System.Convert.ToInt32(configuration[Constants.CHECKINTERVALSECS]);
            Console.WriteLine($"Interval to check for certificates set at {secondsToSleep} seconds");
            //scrape counters    
            var tlsCertsCount = Metrics.CreateGauge("tls_certs_expiry_in_days", "Days remaining for tls certificate expiry", new GaugeConfiguration
            {
                LabelNames = new[] { "secret", "namespace", "domain" }
            });

            var kubernetesConfig = KubernetesClientConfiguration.InClusterConfig();
            var k8sClient = new Kubernetes(kubernetesConfig);
            int refreshCount = 0;

            Console.WriteLine($"Connected as {kubernetesConfig.Username}");

            while (true)
            {
                refreshCount++;
                Console.WriteLine($"Refreshing the metrics : {refreshCount} at {DateTime.UtcNow}");
                try
                {
                    var secrets = await k8sClient.ListSecretForAllNamespacesAsync();
                    Console.WriteLine($"Found {secrets.Items.Count} secrets");
                    var tlsCerts = secrets.Items.Where(a => a.Type.ToLowerInvariant() == Constants.SECRETTYPE).ToList();
                    Console.WriteLine($"Found {tlsCerts.Count} tls");
                    foreach (var cert in tlsCerts)
                    {
                        if (cert.Data.Any(a => a.Key.Contains(Constants.CRTKEY)))
                        {
                            var crtValue = cert.Data.Where(a => a.Key.Contains(Constants.CRTKEY)).First();
                            try
                            {
                                using (var x509Cert = new X509Certificate2(crtValue.Value))
                                {
                                    string hostName = x509Cert.GetNameInfo(X509NameType.DnsName, false);
                                    tlsCertsCount.WithLabels(cert.Metadata.Name, cert.Metadata.NamespaceProperty,
                                        hostName).Set((x509Cert.NotAfter.Date - DateTime.Today).Days);
                                }
                            }
                            catch (System.Exception ex)
                            {
                                Console.WriteLine($"Error converting cert to .net object, exception : {ex.Message}");
                            }
                        }
                    }
                    await Task.Delay(secondsToSleep * 1000);
                    
                }
                catch (System.Exception ex)
                {
                    Console.WriteLine($"Error retriving the secrets, exception : {ex.Message} ");
                }
            }
        }

        private static void SetupRequirements()
        {
            int prometheusPort = System.Convert.ToInt32(configuration[Constants.PROMETHEUSPORT]);

            var metricServer = new MetricServer(port: prometheusPort);
            Console.WriteLine($"Starting metrics server at {prometheusPort}");
            metricServer.Start();
            Console.WriteLine($"Successfully started metrics server at {prometheusPort}");
        }
    }
}
