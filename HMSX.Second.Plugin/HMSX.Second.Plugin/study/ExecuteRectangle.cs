using HMSX.Second.Plugin.Tool;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HMSX.Second.Plugin.study
{
    public class Rectangle
    {
        // 成员变量
        double length;
        double width;
        public Rectangle(double x, double y)
        {
            this.length = x;
            this.width = y;
            Display();
        }
        public void Acceptdetails(double x, double y)
        {
            this.length = x;
            this.width = y;
        }
        public double GetArea()
        {
            return length * width;
        }
        public void Display()
        {
            Console.WriteLine("Length: {0}", length);
            Console.WriteLine("Width: {0}", width);
            Console.WriteLine("Area: {0}", GetArea());
        }
    }
    class ExecuteRectangle
    {
        static void Main1(string[] args)
        {
            int num;
            // KingdeeUtils x = new KingdeeUtils();
            ////var a=x.Getkey("");
            //num = Convert.ToInt32(Console.ReadLine());
            //Console.WriteLine(num);
            Rectangle r = new Rectangle(2, 3);
            //r.Acceptdetails(1,5);
            //r.Display();
            Console.ReadLine();
        }
    }

    interface IMyInterface
    {
        // 接口成员
        void MethodToImplement();
    }

    class InterfaceImplementer : IMyInterface
    {
        static void Main()
        {
            InterfaceImplementer iImp = new InterfaceImplementer();
            iImp.MethodToImplement();
        }

        public void MethodToImplement()
        {
            Console.WriteLine("MethodToImplement() called.");
            Console.ReadLine();
        }
    }
}


