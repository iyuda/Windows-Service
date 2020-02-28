using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace CemsMobilePcrSrv
{
    [Serializable()]
    public class Car
    {
        [System.Xml.Serialization.XmlElement("StockNumber")]
        public string StockNumber { get; set; }

        [System.Xml.Serialization.XmlElement("Make")]
        public string Make { get; set; }

        [System.Xml.Serialization.XmlElement("Model")]
        public string Model { get; set; }
    }


    [Serializable()]
    [System.Xml.Serialization.XmlRoot("CarCollection")]

    public class CarCollection
    {

        [XmlArray("Cars")]
        [XmlArrayItem("Car", typeof(Car))]
        public Car[] Car { get; set; }

        

        public static void Deserialize(string employeeId = null)
        {

            XmlSerializer ser = new XmlSerializer(typeof(CarCollection));
            CarCollection cars;
            string path = File.ReadAllText("c:\\temp\\cars.xml");

            using (StringReader reader = new StringReader(path))
            {
                cars = (CarCollection)ser.Deserialize(reader);
            }

         //   CarCollection cars = null;

            //StreamReader reader = new StreamReader(path);
            //XmlSerializer serializer = new XmlSerializer(typeof(CarCollection));
            //cars = (CarCollection)serializer.Deserialize(reader);
            ////        StringReader reader = new StringReader("<? xml version = \"1.0\" encoding = \"utf-8\" ?>   < Cars > < Car > < StockNumber > 1020 </ StockNumber >         < Make > Nissan </ Make >< Model > Sentra </ Model >   </ Car >  < Car >  < StockNumber > 1010 </ StockNumber >  < Make > Toyota </ Make >    < Model > Corolla </ Model >   </ Car >  < Car >    < StockNumber > 1111 </ StockNumber >    < Make > Honda </ Make >   < Model > Accord </ Model >  </ Car > </ Cars >");
            //////           StreamReader reader = new StreamReader(path);
            ////        cars = (CarCollection)serializer.Deserialize(new StringReader("<? xml version = \"1.0\" encoding = \"utf-8\" ?>   < Cars > < Car > < StockNumber > 1020 </ StockNumber >         < Make > Nissan </ Make >< Model > Sentra </ Model >   </ Car >  < Car >  < StockNumber > 1010 </ StockNumber >  < Make > Toyota </ Make >    < Model > Corolla </ Model >   </ Car >  < Car >    < StockNumber > 1111 </ StockNumber >    < Make > Honda </ Make >   < Model > Accord </ Model >  </ Car > </ Cars >"));
            //Logger.LogActivity(cars.Car[0].StockNumber);
            //reader.Close();
        }
    }
   public static class test_cars
    {
        public static T ParseXML<T>(this string @this) where T : class
        {
            var reader = XmlReader.Create(@this.Trim().ToString(), new XmlReaderSettings() { ConformanceLevel = ConformanceLevel.Document });
            return new XmlSerializer(typeof(T)).Deserialize(reader) as T;
        }
    }
}
