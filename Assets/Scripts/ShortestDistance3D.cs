using System;

/// Finding the shortest distance between two lines
public class ShortestDistance3D
{
    class point
    {
        public double x;
        public double y;
        public double z;
    }

    class line
    {
        public double x1;
        public double y1;
        public double z1;
        public double x2;
        public double y2;
        public double z2;
    }

    double getShortestDistance(line line1, line line2)
    {
        double EPS = 0.00000001;

        point delta21 = new point();
        delta21.x = line1.x2 - line1.x1;
        delta21.y = line1.y2 - line1.y1;
        delta21.x = line1.z2 - line1.z1;

        point delta41 = new point();
        delta41.x = line2.x2 - line2.x1;
        delta41.y = line2.y2 - line2.y1;
        delta41.z = line2.z2 - line2.z1;

        point delta13 = new point();
        delta13.x = line1.x1 - line2.x1;
        delta13.y = line1.y1 - line2.y1;
        delta13.z = line1.z1 - line2.z1;

        double a = dot(delta21, delta21);
        double b = dot(delta21, delta41);
        double c = dot(delta41, delta41);
        double d = dot(delta21, delta13);
        double e = dot(delta41, delta13);
        double D = a * c - b * b;

        double sc, sN, sD = D;
        double tc, tN, tD = D;

        if (D < EPS)
        {
            sN = 0.0;
            sD = 1.0;
            tN = e;
            tD = c;
        }
        else
        {
            sN = (b * e - c * d);
            tN = (a * e - b * d);
            if (sN < 0.0)
            {
                sN = 0.0;
                tN = e;
                tD = c;
            }
            else if (sN > sD)
            {
                sN = sD;
                tN = e + b;
                tD = c;
            }
        }

        if (tN < 0.0)
        {
            tN = 0.0;

            if (-d < 0.0)
                sN = 0.0;
            else if (-d > a)
                sN = sD;
            else
            {
                sN = -d;
                sD = a;
            }
        }
        else if (tN > tD)
        {
            tN = tD;
            if ((-d + b) < 0.0)
                sN = 0;
            else if ((-d + b) > a)
                sN = sD;
            else
            {
                sN = (-d + b);
                sD = a;
            }
        }

        if (Math.Abs(sN) < EPS) sc = 0.0;
        else sc = sN / sD;
        if (Math.Abs(tN) < EPS) tc = 0.0;
        else tc = tN / tD;

        point dP = new point();
        dP.x = delta13.x + (sc * delta21.x) - (tc * delta41.x);
        dP.y = delta13.y + (sc * delta21.y) - (tc * delta41.y);
        dP.z = delta13.z + (sc * delta21.z) - (tc * delta41.z);

        return Math.Sqrt(dot(dP, dP));
    }

    private double dot(point c1, point c2)
    {
        return (c1.x * c2.x + c1.y * c2.y + c1.z * c2.z);
    }

    private double norm(point c1)
    {
        return Math.Sqrt(dot(c1, c1));
    }
}
