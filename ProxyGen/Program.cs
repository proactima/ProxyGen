using System;
using System.Threading;
using Ninject;

namespace ProxyGen
{
    class Program
    {
        static void Main(string[] args)
        {
            var kernel = new StandardKernel(new CodeGenModule());

            try
            {
                var cts = new CancellationTokenSource();
                Console.CancelKeyPress += (s, e) =>
                {
                    e.Cancel = true;
                    cts.Cancel();
                };
                var runner = kernel.Get<ProxyGenerator>();
                runner.RunAsync(cts.Token, args).Wait(cts.Token);
            }
            catch (Exception e)
            {
                Console.WriteLine("Something went wrong:");
                Console.WriteLine(e.Message);
            }

            Console.WriteLine();
            Console.WriteLine("Press enter to exit!");
            Console.ReadLine();
        }
    }
}