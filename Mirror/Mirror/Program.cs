using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using SiteLogic;
using SiteLogic.Domains;

namespace Mirror
{
    public class Program
    {
        public static void Main(string[] args)
        {
            while (true)
            {
                Console.Write("> ");
                string request = Console.ReadLine();
                var worker = CreatePageWorker(request);

                if (worker != null)
                {
                    var task = Task.WhenAll(worker.CreateCopyAsync());
                    task.ContinueWith(t =>
                    {
                        Console.WriteLine("Completed!");
                        Console.Write("> ");
                    });
                }
            }

            Console.ReadKey();
        }

        public static PageWorker CreatePageWorker(string request)
        {
            if (string.IsNullOrWhiteSpace(request))
            {
                return null;
            }

            var requestParams = request.Split(' ');

            var worker = new PageWorker(
                requestParams[0], 
                requestParams[1], 
                int.Parse(requestParams[2]), 
                new NoRestrictionDomainChecker(), 
                null);

            return worker;
        }
    }
}
