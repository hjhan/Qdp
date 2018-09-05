using System;

namespace Qdp.Pricing.Library.Common.MathMethods.Maths
{
    public static class NormalCdf
    {
        // normal CDF using Hart's method
        // see G. West, Better Approximations to Cumulative Normal Functions, Wilmott Magazine, (2005)
        // and Haug, The Complete Guide to Option Pricing Formulas, 2ed, 13.1.1, pp. 465-467
        public static double NormalCdfHart(double x)
        {
            double result = 0;
            double xabs = Math.Abs(x);
            if (xabs > 37)
            {
                result = 0;
            }
            else
            {
                double exponential = Math.Exp(-xabs * xabs / 2);
                if (xabs < 7.07106781186547)  // 10 / sqrt(2)
                {
                    double build = 3.52624965998911E-02 * xabs + 0.700383064443688;
                    build = build * xabs + 6.37396220353165;
                    build = build * xabs + 33.912866078383;
                    build = build * xabs + 112.079291497871;
                    build = build * xabs + 221.213596169931;
                    build = build * xabs + 220.206867912376;
                    result = exponential * build;
                    build = 8.83883476483184E-02 * xabs + 1.75566716318264;
                    build = build * xabs + 16.064177579207;
                    build = build * xabs + 86.7807322029461;
                    build = build * xabs + 296.564248779674;
                    build = build * xabs + 637.333633378831;
                    build = build * xabs + 793.826512519948;
                    build = build * xabs + 440.413735824752;
                    result /= build;
                }
                else
                {
                    double build = xabs + 0.65;
                    build = xabs + 4.0 / build;
                    build = xabs + 3.0 / build;
                    build = xabs + 2.0 / build;
                    build = xabs + 1.0 / build;
                    result = exponential / build / 2.506628274631;
                }
            }

            if (x > 0)
            {
                result = 1 - result;
            }

            return result;
        }

        // see Richard E. Crandall, Topics in Advanced Scientific Computation, p. 85
        // based on Chiarella and Reichel (1968)
        public static double ErfcCrandall(double x)
        {
            const double pi = 3.1415926535897932384626433832795028841971693993751;
            const double tolerance = 1e-200;
            double prec = -Math.Log10(tolerance);
            double result = 0.0;
            double xabs = Math.Abs(x);
            if (xabs < tolerance)
            {
                result = 1;
            }
            else
            {
                double eps = 1 / Math.Sqrt(prec);
                result = 2 / (1.0 - Math.Exp(2 * pi * xabs / eps));
                result += Math.Exp(-xabs * xabs) * eps / (pi * xabs);
                int i = 1;
                double change = 1e20;
                //while (change > tolerance)
                while (Math.Abs(change / result) > 1e-20)
                {
                    double tmp = i * i * eps * eps + xabs * xabs;
                    change = (2 * eps / pi) * x * Math.Exp(-tmp) / tmp;
                    result += change;
                    ++i;
                }
            }

            if (x < 0)
            {
                result = 2 - result;
            }

            return result;
        }

        public static double NormalCdfCrandall(double x)
        {
            const double sqrttwo = 1.4142135623730950488016887242096980785696718753769;
            double xtmp = x / sqrttwo;
            double result = x > 0 ? 1.0 - 0.5 * ErfcCrandall(xtmp) : 0.5 * ErfcCrandall(-xtmp);
            return result;
        }

        public static double NormalCdfWest(double x)
        {
            double XAbs = Math.Abs(x);
            double Cumnorm, Exponential, build;
            if (XAbs > 37)
            {
                Cumnorm = 0;
            }
            else
            {
                Exponential = Math.Exp(-XAbs * XAbs * 0.5);
                if (XAbs < 7.07106781186547)
                {
                    build = 3.52624965998911E-02 * XAbs + 0.700383064443688;
                    build = build * XAbs + 6.37396220353165;
                    build = build * XAbs + 33.912866078383;
                    build = build * XAbs + 112.079291497871;
                    build = build * XAbs + 221.213596169931;
                    build = build * XAbs + 220.206867912376;
                    Cumnorm = Exponential * build;
                    build = 8.83883476483184E-02 * XAbs + 1.75566716318264;
                    build = build * XAbs + 16.064177579207;
                    build = build * XAbs + 86.7807322029461;
                    build = build * XAbs + 296.564248779674;
                    build = build * XAbs + 637.333633378831;
                    build = build * XAbs + 793.826512519948;
                    build = build * XAbs + 440.413735824752;
                    Cumnorm = Cumnorm / build;
                }
                else
                {
                    build = XAbs + 0.65;
                    build = XAbs + 4.0 / build;
                    build = XAbs + 3.0 / build;
                    build = XAbs + 2.0 / build;
                    build = XAbs + 1.0 / build;
                    Cumnorm = Exponential / build / 2.506628274631;
                }
            }
            if (x > 0)
            {
                Cumnorm = 1 - Cumnorm;
            }
            return Cumnorm;
        }




        // Bivariate normal CDF using Genz's method
        // see Haug, The Complete Guide to Option Pricing Formulas, 2ed
        public static double NormalCdfGenz(double x, double y, double rho)
        {
            int i, ISs, LG, NG;
            double h, k, hk, hs, TWOPI;
            double BVN, Ass, asr, sn;
            double A, b, bs, c, d;
            double xs, rs;

            double[,] W;
            double[,] XX;
            W = new double[11, 4];
            XX = new double[11, 4];
            W[1, 1] = 0.1713244923791705;
            XX[1, 1] = -0.9324695142031522;
            W[2, 1] = 0.3607615730481384;
            XX[2, 1] = -0.661209386466265;
            W[3, 1] = 0.46791393457269;
            XX[3, 1] = -0.238619186083197;

            W[1, 2] = 0.0471753363865118;
            XX[1, 2] = -0.981560634246719;
            W[2, 2] = 0.106939325995318;
            XX[2, 2] = -0.904117256370475;
            W[3, 2] = 0.160078328543346;
            XX[3, 2] = -0.769902674194305;
            W[4, 2] = 0.203167426723066;
            XX[4, 2] = -0.587317954286617;
            W[5, 2] = 0.233492536538355;
            XX[5, 2] = -0.36783149899818;
            W[6, 2] = 0.249147045813403;
            XX[6, 2] = -0.125233408511469;

            W[1, 3] = 0.0176140071391521;
            XX[1, 3] = -0.993128599185095;
            W[2, 3] = 0.0406014298003869;
            XX[2, 3] = -0.963971927277914;
            W[3, 3] = 0.0626720483341091;
            XX[3, 3] = -0.912234428251326;
            W[4, 3] = 0.0832767415767048;
            XX[4, 3] = -0.839116971822219;
            W[5, 3] = 0.10193011981724;
            XX[5, 3] = -0.746331906460151;
            W[6, 3] = 0.118194531961518;
            XX[6, 3] = -0.636053680726515;
            W[7, 3] = 0.131688638449177;
            XX[7, 3] = -0.510867001950827;
            W[8, 3] = 0.142096109318382;
            XX[8, 3] = -0.37370608871542;
            W[9, 3] = 0.149172986472604;
            XX[9, 3] = -0.227785851141645;
            W[10, 3] = 0.152753387130726;
            XX[10, 3] = -0.0765265211334973;
            TWOPI = 6.283185307179586;

            if (Math.Abs(rho) < 0.3)
            {
                NG = 1;
                LG = 3;
            }
            else
            {
                if (Math.Abs(rho) < 0.75)
                {
                    NG = 2;
                    LG = 6;
                }
                else
                {
                    NG = 3;
                    LG = 10;
                }
            }

            h = -x;
            k = -y;
            hk = h * k;
            BVN = 0;

            if (Math.Abs(rho) < 0.925)
            {
                if (Math.Abs(rho) > 0)
                {
                    hs = (h * h + k * k) / 2;
                    asr = ArcSin(rho);
                    for (i = 1; i <= LG; i++)
                    {
                        for (ISs = -1; ISs <= 1; ISs += 2)
                        {
                            sn = Math.Sin(asr * (ISs * XX[i, NG] + 1) / 2);
                            BVN = BVN + W[i, NG] * Math.Exp((sn * hk - hs) / (1 - sn * sn));
                        }
                    }
                    BVN = BVN * asr / 2 / TWOPI;
                }
                BVN = BVN + NormalCdfHart(-h) * NormalCdfHart(-k);
            }
            else
            {
                if (rho < 0)
                {
                    k = -k;
                    hk = -hk;
                }
                if (Math.Abs(rho) < 1)
                {
                    Ass = (1 - rho) * (1 + rho);
                    A = Math.Sqrt(Ass);
                    bs = Math.Pow(h - k, 2);
                    c = (4 - hk) / 8;
                    d = (12 - hk) / 16;
                    asr = -(bs / Ass + hk) / 2;
                    if (asr > -100)
                    {
                        BVN = A * Math.Exp(asr) * (1 - c * (bs - Ass) * (1 - d * bs / 5) / 3 + c * d * Ass * Ass / 5);
                    }

                    if (-hk < 100)
                    {
                        b = Math.Sqrt(bs);
                        BVN = BVN - Math.Exp(-hk / 2) * Math.Sqrt(TWOPI) * NormalCdfHart(-b / A) * b
                            * (1 - c * bs * (1 - d * bs / 5) / 3);
                    }
                    A = A / 2;
                    for (i = 1; i <= LG; i++)
                    {
                        for (ISs = -1; ISs <= 1; ISs += 2)
                        {
                            xs = Math.Pow(A * (ISs * XX[i, NG] + 1), 2);
                            rs = Math.Sqrt(1 - xs);
                            asr = -(bs / xs + hk) / 2;
                            if (asr > -100)
                            {
                                BVN = BVN + A * W[i, NG] * Math.Exp(asr)
                                      * (Math.Exp(-hk * (1 - rs) / (2 * (1 + rs))) / rs - (1 + c * xs * (1 + d * xs)));
                            }

                        }

                    }
                    BVN = -BVN / TWOPI;
                }
                if (rho > 0)
                {
                    BVN = BVN + NormalCdfHart(-Math.Max(h, k));
                }
                else
                {
                    BVN = -BVN;
                    if (k > h)
                    {
                        BVN = BVN + NormalCdfHart(k) - NormalCdfHart(h);

                    }
                }
            }
            return BVN;
        }

        public static double ArcSin(double x)
        {
            double A;
            if (Math.Abs(x) == 1)
            {
                A = Math.Sign(x) * Math.PI / 2;
            }
            else
            {
                A = Math.Atan(x / Math.Sqrt(1 - x * x));
            }
            return A;
        }


        public static double NormSInv(double U)
        {
            double a1 = -39.6968302866538;
            double a2 = 220.946098424521;
            double a3 = -275.928510446969;

            double a4 = 138.357751867269, a5 = -30.6647980661472, a6 = 2.50662827745924;
            double b1 = -54.4760987982241, b2 = 161.585836858041, b3 = -155.698979859887;
            double b4 = 66.8013118877197, b5 = -13.2806815528857, c1 = -7.78489400243029E-03;
            double c2 = -0.322396458041136, c3 = -2.40075827716184, c4 = -2.54973253934373;
            double c5 = 4.37466414146497, c6 = 2.93816398269878, d1 = 7.78469570904146E-03;
            double d2 = 0.32246712907004, d3 = 2.445134137143, d4 = 3.75440866190742;
            double p_low = 0.02425, p_high = 1 - p_low;
            double q, r, NormSInv;

            if (U < 0 | U > 1)
            {
                NormSInv = 0;
            }
            else
            {
                if (U < p_low)
                {
                    q = Math.Sqrt(-2 * Math.Log(U));
                    NormSInv = (((((c1 * q + c2) * q + c3) * q + c4) * q + c5) * q + c6) / ((((d1 * q + d2) * q + d3) * q + d4) * q + 1);
                }
                else
                {
                    if (U <= p_high)
                    {
                        q = U - 0.5;
                        r = q * q;
                        NormSInv = (((((a1 * r + a2) * r + a3) * r + a4) * r + a5) * r + a6) * q / (((((b1 * r + b2) * r + b3) * r + b4) * r + b5) * r + 1);
                    }
                    else
                    {
                        q = Math.Sqrt(-2 * Math.Log(1 - U));
                        NormSInv = -(((((c1 * q + c2) * q + c3) * q + c4) * q + c5) * q + c6) / ((((d1 * q + d2) * q + d3) * q + d4) * q + 1);
                    }
                }
            }

            return NormSInv;

        }



        public static double NormalBiCdfWest(double a, double b, double r)
        {
            int i;
            double[] x, W;
            double h1, h2, LH = 0.0, h12, h3, h5, h6, h7, r1, r2, r3, rr, AA, ab, Bivarcumnorm;
            x = new double[] { 0.04691008, 0.23076534, 0.5, 0.76923466, 0.95308992 };
            W = new double[] { 0.018854042, 0.038088059, 0.0452707394, 0.038088059, 0.018854042 };
            h1 = a;
            h2 = b;
            h12 = (h1 * h1 + h2 * h2) / 2.0;
            if (Math.Abs(r) >= 0.7)
            {
                r2 = 1 - r * r;
                r3 = Math.Sqrt(r2);
                if (r < 0) { h2 = -h2; };
                h3 = h1 * h2;
                h7 = Math.Exp(-h3 / 2);
                if (Math.Abs(r) < 1)
                {
                    h6 = Math.Abs(h1 - h2);
                    h5 = h6 * h6 / 2.0;
                    h6 = h6 / r3;
                    AA = 0.5 - h3 / 8.0;
                    ab = 3.0 - 2.0 * AA * h5;
                    LH = 0.13298076 * h6 * ab * (1 - NormalCdfWest(h6)) - Math.Exp(-h5 / r2) * (ab + AA * r2) * 0.053051647;

                    for (i = 0; i <= 4; i++)
                    {
                        r1 = r3 * x[i];
                        rr = r1 * r1;
                        r2 = Math.Sqrt(1 - rr);
                        LH = LH - W[i] * Math.Exp(-h5 / rr) * (Math.Exp(-h3 / (1 + r2)) / r2 / h7 - 1 - AA * rr);
                    }
                }
                Bivarcumnorm = LH * r3 * h7 + NormalCdfWest(Math.Min(h1, h2));
                if (r < 0)
                {
                    Bivarcumnorm = NormalCdfWest(h1) - Bivarcumnorm;
                }
            }
            else
            {
                h3 = h1 * h2;
                if (r != 0)
                {
                    for (i = 0; i <= 4; i++)
                    {
                        r1 = r * x[i];
                        r2 = 1 - r1 * r1;
                        LH = LH + W[i] * Math.Exp((r1 * h3 - h12) / r2) / Math.Sqrt(r2);
                    }
                }
                Bivarcumnorm = NormalCdfWest(h1) * NormalCdfWest(h2) + r * LH;
            }
            return Bivarcumnorm;
        }



        public static double NormalPdf(double x, double mu = 0, double sigma = 1)
        {
            return Math.Exp(-Math.Pow((x - mu), 2) / 2 / sigma / sigma) / Math.Sqrt(2 * Math.PI * sigma * sigma);
        }

    }
}




    

