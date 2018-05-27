using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

public enum Operands
{
    [XmlEnum(Name = "add")]
    Add,
    [XmlEnum(Name = "multiply")]
    Multiply,
    [XmlEnum(Name = "divide")]
    Divide,
    [XmlEnum(Name = "subtract")]
    Subtract

}

public class Element
{

    [XmlAttribute(AttributeName = "name")]
    public string name { get; set; } = "";

    [XmlAttribute(AttributeName = "value")]
    public string value { get; set; } = "";
}

public class Operand
{

    [XmlAttribute(AttributeName = "name")]
    public string name { get; set; }

    [XmlAttribute(AttributeName = "value")]
    public Operands value { get; set; }
}

[Serializable()]
public class Calculation
{
    [XmlElement("str", Order = 1)]
    public Element Uid { get; set; }

    [XmlElement("str", Order = 2)]
    public Operand Operand { get; set; }

    [XmlElement("int", Order = 3)]
    public Element Mod { get; set; }
}


[Serializable()]
[System.Xml.Serialization.XmlRoot("folder")]
public class Calculations
{
    [XmlArray("folder")]
    [XmlArrayItem("folder", typeof(Calculation))]
    public Calculation[] Calcs { get; set; }

    private static int Priority(Operands input)
    {
        switch (input)
        {
            case Operands.Add:
                return 1;
            case Operands.Multiply:
                return 2;
            case Operands.Divide:
                return 2;
            case Operands.Subtract:
                return 1;
            default: return 0;
        }
    }

    private float CalculateRecursion(List<Calculation> cals)
    {
        if (cals.Count == 1)
            return float.Parse(cals[0].Mod.value);


        int highestPriority = 0;
        int index = 0;
        for (int i = 0; i < cals.Count - 1; i++)
        {
            if (Priority(cals[i + 1].Operand.value) > highestPriority)
            {
                highestPriority = Priority(cals[i + 1].Operand.value);
                index = i;
                //if (string.IsNullOrEmpty(cals[index + 1].Mod.value))
                //{
                //    //Console.WriteLine($"element with id { cals[index].Uid.value} has wrong value\n");
                //    continue;
                //}
            }
        }

        if (highestPriority == 1)
        {

            float leftOperand = 0;
            float rightOperand = 0;
            float.TryParse(cals[index].Mod.value, NumberStyles.Number, CultureInfo.InvariantCulture, out leftOperand);
            float.TryParse(cals[index + 1].Mod.value, NumberStyles.Number, CultureInfo.InvariantCulture, out rightOperand);

            float innerResult = 0;
            if (cals[index + 1].Operand.value == Operands.Add)
                innerResult = leftOperand + rightOperand;
            else
                innerResult = leftOperand - rightOperand;
            cals[index].Mod.value = innerResult.ToString();
            cals.RemoveAt(index + 1);
            return CalculateRecursion(cals);
        }


        else if (highestPriority == 2)
        {

            float leftOperand = 0;
            float rightOperand = 0;
            float.TryParse(cals[index].Mod.value, NumberStyles.Number, CultureInfo.InvariantCulture, out leftOperand);
            float.TryParse(cals[index + 1].Mod.value, NumberStyles.Number, CultureInfo.InvariantCulture, out rightOperand);

            float innerResult = 0;
            if (cals[index + 1].Operand.value == Operands.Multiply)
                innerResult = leftOperand * rightOperand;
            else
                innerResult = leftOperand / rightOperand;
            cals[index].Mod.value = innerResult.ToString();
            cals.RemoveAt(index + 1);
            return CalculateRecursion(cals);
        }

        return 0;
    }




    public float Calculate()
    {
        List<Calculation> tmpCalcs = Calcs.ToList();
        return CalculateRecursion(tmpCalcs);
    }
}

namespace XmlDeser
{



    class Program
    {

        private static Calculations DeserializeFile(string file)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(Calculations));
            StreamReader reader = new StreamReader(file);
            Calculations calcs = (Calculations)serializer.Deserialize(reader);
            reader.Close();
            return calcs;
        }
        public class CalulationsDeserializer
        {
            private int filesPerThread = 137;

            public CalulationsDeserializer(string path, int filesPerThread = 50)
            {
                this.filesPerThread = filesPerThread;

                List<Thread> workerThreads = new List<Thread>();
                string[] files = Directory.GetFiles(path, "*.xml");
                int maxCalcs = 0;
                string maxCalcsFile = "";
                for (int i = 0; i < files.Length - filesPerThread; i += filesPerThread)
                {
                    Parallel.For(i, i + filesPerThread, j =>
                    {

                        Calculations calcs = DeserializeFile(files[j]);
                        float res = calcs.Calculate();
                        if (calcs.Calcs.Count() > maxCalcs)
                        {
                            maxCalcs = calcs.Calcs.Count();
                            maxCalcsFile = Path.GetFileName(files[j]);
                        }
                        Console.WriteLine($"File name: {Path.GetFileName(files[j])}, File result: {res}");
                    });
                }
                //Console.WriteLine(filesPerThread * (files.Length / filesPerThread));
                Parallel.For(filesPerThread * (files.Length / filesPerThread), files.Length, j =>
                {
                    Calculations calcs = DeserializeFile(files[j]);
                    float res = calcs.Calculate();
                    if (calcs.Calcs.Count() > maxCalcs)
                    {
                        maxCalcs = calcs.Calcs.Count();
                        maxCalcsFile = Path.GetFileName(files[j]);
                    }
                    Console.WriteLine($"File name: {Path.GetFileName(files[j])}, File result: {res}");
                });

                Console.WriteLine($"\nFile with max calculations {maxCalcsFile}, number of calculations {maxCalcs}");
            }


        }


        static void Main(string[] args)
        {
            if (args.Length == 1)
            {

            }
            var watch = System.Diagnostics.Stopwatch.StartNew();

            var xmlDeser = new CalulationsDeserializer("C:\\Users\\lol\\source\\repos\\XmlDeser\\bin\\Debug\\Test");
            watch.Stop();
            Console.WriteLine($"Executuion time : {watch.ElapsedMilliseconds / 1000.0f}s\n");
        }
    }
}
