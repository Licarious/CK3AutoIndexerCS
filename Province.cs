﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK3AutoIndexerCS
{
    internal class Province
    {
        public int id = -1;
        public string name = "";
        public string otherInfo = "";
        public Color color;
        public HashSet<(int, int)> coords = new();
        public Province? newProv = null;

        public Province(int id, int r, int g, int b, string name) { 
            this.id = id;
            this.name = name;
            this.color = Color.FromArgb(r, g, b);
            //this.otherInfo = otherInfo;
        }

        
    }
}
