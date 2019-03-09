using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using SiteLogic;

namespace Mirror
{
    public class Program
    {
        public static void Main(string[] args)
        {
            SiteWorker worker = new SiteWorker("https://gidonline.io/", @"E:\Epam\epam-lab\Sites\", 0);

            Task.WaitAll(worker.CreateLocalCopy());

            Console.WriteLine("Completed!");
            Console.ReadKey();
        }
    }
}
