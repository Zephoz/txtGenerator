using System;
using System.IO;
using System.Collections.Generic;

namespace txtGenerator
{
    public class Star
    {
        int screenX = 2560;
        int screenY = 1600;
        // Properties
        int id;
        int posX;
        int posY;
        int radius;
        int age;
        Char activity;
        String colour;
        
        public int _id
        {
            get { return id; }
            set { id = value; }
        }
        
        public int PosX
        {
            get { return posX; }
            set
            {
                if (value > screenX)
                    value = screenX;
                if (value < 0)
                    value = 0;
                posX = value;
            }
        }
        
        public int PosY
        {
            get { return posY; }
            set
            {
                if (value > screenY)
                    value = screenY;
                if (value < 0)
                    value = 0;
                posY = value;
            }
        }
        
        public int Radius
        {
            get { return radius; }
            set { radius = value; }
        }
        
        public int Age
        {
            get { return age; }
            set { age = value; }
        }
        
        public Char Activity
        {
            get { return activity; }
            set { activity = value; }
        }
        
        public String Colour
        {
            get { return colour; }
            set
            {
                while (value.Length > 6)
                    value.Remove(6);
                colour = value;
            }
        }

        public Star()
        {

        }

        public Star(int _id, int PosX, int PosY, int Radius, int Age, Char Activity, String Colour)
        {
            this._id = _id;
            this.PosX = PosX;
            this.PosY = PosY;
            this.Activity = Activity;
            this.Colour = Colour;
            this.Radius = Radius;
            this.Age = Age;
        }

        public override String ToString()
        {
            String s = String.Format("{0}|{1}|{2}|{3}|{4}|{5}|{6}", this._id, this.PosX, this.PosY, this.Radius, this.Colour, this.Activity, this.Age);
            return s;
        }

        public static List<Star> ToStar(String filename)
        {
            List<Star> stars = new List<Star>();
            StreamReader sr = new StreamReader(filename);
            String s = sr.ReadToEnd();
            String[] x = s.Split('\r', '\n');

            foreach (String i in x)
            {
                if (i != "")
                {
                    String[] t = i.Split('|');
                    stars.Add(new Star(Convert.ToInt32(t[0]), Convert.ToInt16(t[1]), Convert.ToInt16(t[2]), Convert.ToInt16(t[3]), Convert.ToInt32(t[6]), Convert.ToChar(t[5]), t[4]));
                }
            }
            sr.Close();
            return stars;

        }

        public static new Type GetType()
        {
            return typeof(Star);
        }
    }
}
