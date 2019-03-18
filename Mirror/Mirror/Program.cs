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
            var worker = new PageWorker(
                "https://gidonline.io/new/", 
                @"E:\Epam\epam-lab\Sites\", 
                1, 
                new HigherUrlDomainChecker(), 
                null);

            Task.WaitAll(worker.CreateCopyAsync());

            Console.WriteLine("Completed!");
            Console.ReadKey();
        }
    }
}
