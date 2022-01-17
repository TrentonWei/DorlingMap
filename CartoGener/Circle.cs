using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CartoGener
{
    /// <summary>
    /// Circle symbol
    /// </summary>
    class Circle
    {
        int ID;//ID
        public double Radius;//Radius
        public double CenterX;//X
        public double CenterY;//Y
        public double Value;//the Value it represents
        public double scale;
        public string Name;

        public Circle(int ID)
        {
            this.ID = ID;
        }
    }
}
