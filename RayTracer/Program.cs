using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

public struct Vec
{
    public double x, y, z;              // position, also color (r,g,b)
    public Vec(double x_ = 0, double y_ = 0, double z_ = 0) { x = x_; y = y_; z = z_; }
    public static Vec operator +(Vec a, Vec b) { return new Vec(a.x + b.x, a.y + b.y, a.z + b.z); }
    public static Vec operator -(Vec a, Vec b) { return new Vec(a.x - b.x, a.y - b.y, a.z - b.z); }
    public static Vec operator *(Vec a, double b) { return new Vec(a.x * b, a.y * b, a.z * b); }
    public Vec mult(Vec b) { return new Vec(x * b.x, y * b.y, z * b.z); }
    public Vec norm() { return this * (1 / Math.Sqrt(x * x + y * y + z * z)); }
    public double dot(Vec b) { return x * b.x + y * b.y + z * b.z; } // cross:
    public static Vec operator %(Vec a, Vec b) { return new Vec(a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x); }
}

public struct Ray { public Vec o, d; public Ray(Vec o_, Vec d_) { o = o_; d = d_; } }

public enum Refl_t { DIFF, SPEC, REFR };  // material types, used in radiance()

public struct Sphere
{
    public double rad;       // radius
    public Vec p, e, c;      // position, emission, color
    public Refl_t refl;      // reflection type (DIFFuse, SPECular, REFRactive)
    public Sphere(double rad_, Vec p_, Vec e_, Vec c_, Refl_t refl_)
    {
        rad = rad_; p = p_; e = e_; c = c_; refl = refl_;
    }
    public double intersect(Ray r)
    { // returns distance, 0 if nohit
        Vec op = p - r.o; // Solve t^2*d.d + 2*t*(o-p).d + (o-p).(o-p)-R^2 = 0
        double t, eps = 1e-4, b = op.dot(r.d), det = b * b - op.dot(op) + rad * rad;
        if (det < 0) return 0; else det = Math.Sqrt(det);
        return (t = b - det) > eps ? t : ((t = b + det) > eps ? t : 0);
    }
}

internal static class Program
{
    static Sphere[] spheres = new[] { //Scene: radius, position, emission, color, material 
        new Sphere(1e5, new Vec( 1e5+1,40.8,81.6), new Vec(),new Vec(.75,.25,.25),Refl_t.DIFF),//Left
        new Sphere(1e5, new Vec(-1e5+99,40.8,81.6),new Vec(),new Vec(.25,.25,.75),Refl_t.DIFF),//Rght
        new Sphere(1e5, new Vec(50,40.8, 1e5),     new Vec(),new Vec(.75,.75,.75),Refl_t.DIFF),//Back
        new Sphere(1e5, new Vec(50,40.8,-1e5+170), new Vec(),new Vec(),           Refl_t.DIFF),//Frnt
        new Sphere(1e5, new Vec(50, 1e5, 81.6),    new Vec(),new Vec(.75,.75,.75),Refl_t.DIFF),//Botm
        new Sphere(1e5, new Vec(50,-1e5+81.6,81.6),new Vec(),new Vec(.75,.75,.75),Refl_t.DIFF),//Top
        new Sphere(16.5,new Vec(27,16.5,47),       new Vec(),new Vec(1,1,1)*.999, Refl_t.SPEC),//Mirr
        new Sphere(16.5,new Vec(73,16.5,78),       new Vec(),new Vec(1,1,1)*.999, Refl_t.REFR),//Glas
        new Sphere(600, new Vec(50,681.6-.27,81.6),new Vec(12,12,12),  new Vec(), Refl_t.DIFF) //Lite
    };
    static double clamp(double x) { return x < 0 ? 0 : x > 1 ? 1 : x; }
    static int toInt(double x) { return (int)(Math.Pow(clamp(x), 1 / 2.2) * 255 + .5); }
    static bool intersect(Ray r, ref double t, ref int id)
    {
        double inf = t = 1e20, d;
        for (int i = spheres.Length - 1; i >= 0; i--) { d = spheres[i].intersect(r); if (d > 0 && d < t) { t = d; id = i; } }
        return t < inf;
    }
    static Vec radiance(Ray r, int depth, Random random)
    {
        double t = 0;                           // distance to intersection
        int id = 0;                             // id of intersected object
        if (!intersect(r, ref t, ref id)) return new Vec(); // if miss, return black
        Sphere obj = spheres[id];             // the hit object
        Vec x = r.o + r.d * t, n = (x - obj.p).norm(), nl = n.dot(r.d) < 0 ? n : n * -1, f = obj.c;
        double p = f.x > f.y && f.x > f.z ? f.x : f.y > f.z ? f.y : f.z; // max refl
        if (depth > 100)
        {
            return obj.e; // *** Added to prevent stack overflow
        }
        if (++depth > 5) if (random.NextDouble() < p) f = f * (1 / p); else return obj.e; //R.R.
        if (obj.refl == Refl_t.DIFF)
        {         // Ideal DIFFUSE reflection
            double r1 = 2 * Math.PI * random.NextDouble(), r2 = random.NextDouble(), r2s = Math.Sqrt(r2);
            Vec w = nl, u = ((Math.Abs(w.x) > .1 ? new Vec(0, 1) : new Vec(1)) % w).norm(), v = w % u;
            Vec d = (u * Math.Cos(r1) * r2s + v * Math.Sin(r1) * r2s + w * Math.Sqrt(1 - r2)).norm();
            return obj.e + f.mult(radiance(new Ray(x, d), depth, random));
        }
        else if (obj.refl == Refl_t.SPEC) // Ideal SPECULAR reflection
            return obj.e + f.mult(radiance(new Ray(x, r.d - n * 2 * n.dot(r.d)), depth, random));
        Ray reflRay = new Ray(x, r.d - n * 2 * n.dot(r.d));// Ideal dielectric REFRACTION
        bool into = n.dot(nl) > 0;                   // Ray from outside going in?
        double nc = 1, nt = 1.5, nnt = into ? nc / nt : nt / nc, ddn = r.d.dot(nl), cos2t;
        if ((cos2t = 1 - nnt * nnt * (1 - ddn * ddn)) < 0)        // Total internal reflection
            return obj.e + f.mult(radiance(reflRay, depth, random));
        Vec tdir = (r.d * nnt - n * ((into ? 1 : -1) * (ddn * nnt + Math.Sqrt(cos2t)))).norm();
        double a = nt - nc, b = nt + nc, R0 = a * a / (b * b), c = 1 - (into ? -ddn : tdir.dot(n));
        double Re = R0 + (1 - R0) * c * c * c * c * c, Tr = 1 - Re, P = .25 + .5 * Re, RP = Re / P, TP = Tr / (1 - P);
        return obj.e + f.mult(depth > 2 ? (random.NextDouble() < P ?   // Russian roulette
          radiance(reflRay, depth, random) * RP
          : radiance(new Ray(x, tdir), depth, random) * TP)
          : radiance(reflRay, depth, random) * Re + radiance(new Ray(x, tdir), depth, random) * Tr);
    }
    private static void Main(string[] args)
    {
        Console.Write("Enter number of samples: ");
        int sampsInput = int.Parse(Console.ReadLine());
        int w = 1024, h = 768, samps = sampsInput / 4; // # samples
        Ray cam = new Ray(new Vec(50, 52, 295.6), new Vec(0, -0.042612, -1).norm()); // cam pos, dir
        Vec cx = new Vec(w * .5135 / h), cy = (cx % cam.d).norm() * .5135;
        var sample = new Func<Random, int, Vec>((random, i) => {
            var x = i % w;
            var y = h - i / w - 1;
            var color = new Vec();
            for (int sy = 0; sy < 2; sy++)
            { // 2x2 subpixel rows
                for (int sx = 0; sx < 2; sx++)
                {              // 2x2 subpixel cols
                    Vec r = new Vec();
                    for (int s = 0; s < samps; s++)
                    {
                        double r1 = 2 * random.NextDouble(), dx = r1 < 1 ? Math.Sqrt(r1) - 1 : 1 - Math.Sqrt(2 - r1);
                        double r2 = 2 * random.NextDouble(), dy = r2 < 1 ? Math.Sqrt(r2) - 1 : 1 - Math.Sqrt(2 - r2);
                        var v0 = cx * (((sx + .5 + dx) / 2 + x) / w - .5);
                        var v1 = cy * (((sy + .5 + dy) / 2 + y) / h - .5);
                        Vec d = cx * (((sx + .5 + dx) / 2 + x) / w - .5) +
                                cy * (((sy + .5 + dy) / 2 + y) / h - .5) + cam.d;
                        d = d.norm();
                        r = r + radiance(new Ray(cam.o + d * 140, d), 0, random) * (1.0 / samps);
                    } // Camera rays are pushed ^^^^^ forward to start in interior
                    color = color + new Vec(clamp(r.x), clamp(r.y), clamp(r.z)) * .25;
                }
            }
            return color;
        });

        var c = new Vec[w * h];

        // First do the sequential version.
        Console.WriteLine("Executing sequential loop...");
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        var random = new Random(12345);
        for (int i = 0; i < (w * h); i++)
        {
            c[i] = sample(random, i);
        }
        stopwatch.Stop();
        Console.WriteLine("Sequential loop time in milliseconds: {0}",
                                stopwatch.Elapsed);

        // Reset timer and results matrix.
        stopwatch.Reset();
        c = new Vec[w * h];

        // Do the parallel loop.
        Console.WriteLine("Executing parallel loop...");
        stopwatch.Start();
        var randomTL = new ThreadLocal<Random>(() => new Random(12345));
        Parallel.For(0, w * h, i => c[i] = sample(randomTL.Value, i));
        stopwatch.Stop();
        Console.WriteLine("Parallel loop time in milliseconds: {0}",
                                stopwatch.Elapsed);

        // Write image to PPM file.
        var sw = File.CreateText("smallptCS.ppm"); 
        sw.Write("P3\n{0} {1}\n{2}\n", w, h, 255);
        for (int i = 0; i < w * h; i++)
        {
            sw.Write("{0} {1} {2}\n", toInt(c[i].x), toInt(c[i].y), toInt(c[i].z));
        }

        // Keep the console window open in debug mode.
        Console.WriteLine("Press any key to exit.");
        Console.ReadKey();
    }
}