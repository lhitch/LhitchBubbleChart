using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LhitchBubbleChart
{
    class CircleData 
    {
        public float value;
        public string name;
        public bool hasPos;
        public float positionX;//x coord of circle center
        public float positionY;//y coord of circle center

        public CircleData()
        {
            hasPos = false;
            positionX = 0;
            positionY = 0;
        }
    }
}
