using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Net.Http;
using System.Threading;
using InfluxCsvReal.Models;
using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;
using Microsoft.AspNetCore.Hosting;
using InfluxDB.Client.Core;
using System.Security.Policy;

namespace InfluxCsvReal.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class InfluxCsvController : ControllerBase
    {
        class InfluxDbConfig
        {
            public string Token;
            public string BucketName;
            public string Org;
            public string Url;
        }


        private IWebHostEnvironment _hostingEnvironment;
        //InfluxDBClient _InfluxDbClient;


        InfluxDbConfig _DbConfig = new();
        InfluxDbConfig _EC2Config = new();


        private readonly ILogger<InfluxCsvController> _Logger;

        public InfluxCsvController(ILogger<InfluxCsvController> logger, IWebHostEnvironment environment)
        {
            _Logger = logger;
            _hostingEnvironment = environment;

            _EC2Config.BucketName = "";
            _EC2Config.Org = "";
            _EC2Config.Url = "";
            _EC2Config.Token =
                "";

        }

        [HttpPost]
        [Route("RunAndRelease")]
        public async Task runAndRelease()
        {
            var cd = Directory.GetCurrentDirectory();

            var path = Path.Combine(cd, "CsvLounge");

            var filesInLounge = Directory.GetFiles(path);

            var indexCounter = 1;
            foreach (var file in filesInLounge)
                await CreateCsvs(file);
            
            Console.WriteLine("END OF THE LIST. FINISHED WITH UPLOADING CSV DATA");
        }



        [HttpPost]
        [Route("CreateCSVs")]
        public async Task CreateCsvs(string path)
        {
            string line;
            var totalRows = -1;
            StreamReader thisFile = new StreamReader(path);
            
            while (!string.IsNullOrWhiteSpace(line = thisFile.ReadLine())) //loop retrieves TOTAL ROWS in file
                totalRows++;

            thisFile = new StreamReader(path);
            string headerLine = thisFile.ReadLine();
            var headers = headerLine.Split(',');
            var startTime = DateTime.Now;
            var row = 0;
            var columns = 0;
            var loadingBarrier = 10;
            List<TimeValueModel> values = new();
            
            while (!string.IsNullOrWhiteSpace(line =
                       thisFile.ReadLine())) //looping through each LINE == ROW in csv file
            {
                var cells = line.Split(",");
                var count = cells.Count();
                columns = count;


                for (var i = 0; i < count - 1; i++) // looping through each COUNT == COLUMN in csv file
                {
                    var tv = new TimeValueModel();
                    //"dd/MM/yyyy HH:mm:ss:ffff
                    tv.Column = i;
                    tv.Title = headers[i];
                    try
                    {
                        tv.Time = DateTime.ParseExact(cells[0], "M/dd/yy HH:mm", CultureInfo.InvariantCulture).ToUniversalTime();
                        //var dt = DateTime.Parse(cells[0]);
                        //tv.Time = dt.ToUniversalTime();
                    }
                    catch
                    {
                        Console.WriteLine("date parse failed");
                        tv.Time = null;
                    }

                    try
                    {
                        tv.Value = double.Parse(cells[i]);
                    }
                    catch (Exception e)
                    {
                        tv.Value = i;
                    }

                    if (tv.Title != headers[0])
                        values.Add(tv);
                }

                row++;
                string progressTotal = "";
                double fraction = (double)row / (double)totalRows;
                if (row % 20 == 0)
                {
                    var cents = fraction * 100;

                    for (int i = 0; i < Math.Floor(cents); i++)
                        progressTotal += "#";

                    Console.Clear();
                    Console.Write($"{cents.ToString("0.00")}%  [{progressTotal}]\n");

                    if (cents >= loadingBarrier || loadingBarrier >99)
                    {
                        loadingBarrier += loadingBarrier;
                        //update db
                        for (var d = 0; d < columns; d++)
                        {
                            var col = values.Where(x => x.Column == d).ToList();
                            if (col.Count > 10000)
                            {
                                var chunkedCol = SplitList(col, 5000);
                                foreach (var c in chunkedCol)
                                    Console.WriteLine("insertedtodb");
                                        //await InsertManyEC2(c);
                            }
                            else
                                Console.WriteLine("insertedtodb");

                            //await InsertManyEC2(col);


                            var progressDb = "";
                            double FractionDb = (double)d / (double)columns;
                            var centsDb = Math.Round(FractionDb * 100);
                            for (int w = 0; w < centsDb / 2; w++)
                                progressDb += "#";

                            Console.Clear();
                            Console.WriteLine($"{d}/ {columns} {centsDb.ToString("0.00")}% of Columns to Db      -{cents.ToString("0.00")}% Completed-\n");
                            Console.WriteLine($"[{progressDb}]\n");


                        }

                        values.Clear();
                        Console.WriteLine("Db Finished Updating\n");
                        var TimeMins = DateTime.Now.Subtract(startTime).TotalMinutes * (100 - cents);
                        var eta = DateTime.Now.AddMinutes(TimeMins);
                        Console.WriteLine("ETA: {0}", eta);
                    }
                }
            }

        }

        [HttpPost]
        [Route("SplitList")]
        public IEnumerable<List<T>> SplitList<T>(List<T> bigList, int nSize = 4)
        {
            for (int i = 0; i < bigList.Count; i += nSize)
            {
                yield return bigList.GetRange(i, Math.Min(nSize, bigList.Count - i));
            }
        }


        [HttpPost]
        [Route("InsertManyEC2")]
        public async Task InsertManyEC2(List<TimeValueModel> data)
        {
            using var client = InfluxDBClientFactory.Create(_EC2Config.Url, _EC2Config.Token.ToCharArray());

            var writeApi = client.GetWriteApiAsync();

            await writeApi.WriteMeasurementsAsync(data, WritePrecision.Ms, _EC2Config.BucketName, _EC2Config.Org);

        }
    }
}