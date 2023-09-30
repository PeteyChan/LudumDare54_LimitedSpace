using System;

namespace Internal
{
    public partial class Generators
    {
        //static void Gen(Debug.Console args) => Results(args.ToInt(0));

        public static void GenResults(int arg_count)
        {
            var file = "_Core/Utilities/Result.cs";
            var file_gen = new Utils.CodeGen_Helper();
            file_gen.AddPendingFile(file);

            var template = file_gen.GetBetween("//Template", false);
            file_gen.WriteBetween("//Generated", true, () =>
            {
                var gen = new Utils.CodeGen_Helper();

                for (int args = 2; args < arg_count + 1; ++args)
                {
                    // Union<T1, T2>
                    string result_type = $"Result<{gen.Pattern(1, args + 1, i => $"T{i}")}>";

                    gen.Clear();
                    gen.AddPendingText(template);
                    // write header
                    gen.WriteUntil("//Header", false);
                    gen.WriteLine($"public struct {result_type}");

                    gen.WriteUntil("//Fields", false);
                    gen.WriteLine(gen.Pattern(1, args + 1, i => $"T{i} t{i};", ""));

                    // write match signature
                    // public Union<T1, T2> Match(Action<T1> match_t1, Action<T2> match_t2, Action none)
                    gen.WriteUntil("//MatchSig", false);
                    gen.WriteLine($"public T Match<T>({gen.Pattern(1, args + 1, i => $"Func<T{i}, T> match_t{i}")}, Func<System.Exception, T> error)");

                    gen.WriteUntil("//Match", false);
                    //1 => match_t1(t1),
                    for (int i = 1; i < args + 1; ++i)
                        gen.WriteLine($"{i} => match_t{i}(t{i}),");


                    // public static implicit operator Result<T1>(T1 t1) => new Result<T1> { type = 1, t1 = t1 };
                    gen.WriteBetween("//Implicit", false, () =>
                    {
                        for (int i = 1; i < args + 1; ++i)
                            gen.WriteLine($"public static implicit operator {result_type}(T{i} t{i}) => new {result_type}" + "{" + $" type = {i} , t{i} = t{i}" + "};");
                        gen.WriteLine($"public static implicit operator {result_type}(System.Exception exception) => new {result_type}" + "{ exception = exception };");
                    });

                    gen.WriteRest();
                    file_gen.WriteCodeGen(gen);
                }

            });
            file_gen.WriteRest();
            file_gen.WriteToFile(file);
        }
    }
}

namespace Utils
{
    //Template
    public struct Result<T1>//Header
    {
        byte type;
        T1 t1; //Fields
        System.Exception exception;

        public T Match<T>(Func<T1, T> match_t1, Func<System.Exception, T> error)//MatchSig
            => type switch
            {
                1 => match_t1(t1), //Match
                _ => error(exception == null ? new System.NullReferenceException() : exception),
            };

        //Implicit
        public static implicit operator Result<T1>(T1 t1) => new Result<T1> { type = 1, t1 = t1 };
        public static implicit operator Result<T1>(System.Exception exception) => new Result<T1> { exception = exception };
        //Implicit
    }
    //Template

    //Generated
    public struct Result<T1, T2>
    {
        byte type;
        T1 t1; T2 t2;
        System.Exception exception;

        public T Match<T>(Func<T1, T> match_t1, Func<T2, T> match_t2, Func<System.Exception, T> error)
                    => type switch
                    {
                        1 => match_t1(t1),
                        2 => match_t2(t2),
                        _ => error(exception == null ? new System.NullReferenceException() : exception),
                    };

        public static implicit operator Result<T1, T2>(T1 t1) => new Result<T1, T2> { type = 1, t1 = t1 };
        public static implicit operator Result<T1, T2>(T2 t2) => new Result<T1, T2> { type = 2, t2 = t2 };
        public static implicit operator Result<T1, T2>(System.Exception exception) => new Result<T1, T2> { exception = exception };
    }
    public struct Result<T1, T2, T3>
    {
        byte type;
        T1 t1; T2 t2; T3 t3;
        System.Exception exception;

        public T Match<T>(Func<T1, T> match_t1, Func<T2, T> match_t2, Func<T3, T> match_t3, Func<System.Exception, T> error)
                    => type switch
                    {
                        1 => match_t1(t1),
                        2 => match_t2(t2),
                        3 => match_t3(t3),
                        _ => error(exception == null ? new System.NullReferenceException() : exception),
                    };

        public static implicit operator Result<T1, T2, T3>(T1 t1) => new Result<T1, T2, T3> { type = 1, t1 = t1 };
        public static implicit operator Result<T1, T2, T3>(T2 t2) => new Result<T1, T2, T3> { type = 2, t2 = t2 };
        public static implicit operator Result<T1, T2, T3>(T3 t3) => new Result<T1, T2, T3> { type = 3, t3 = t3 };
        public static implicit operator Result<T1, T2, T3>(System.Exception exception) => new Result<T1, T2, T3> { exception = exception };
    }
    public struct Result<T1, T2, T3, T4>
    {
        byte type;
        T1 t1; T2 t2; T3 t3; T4 t4;
        System.Exception exception;

        public T Match<T>(Func<T1, T> match_t1, Func<T2, T> match_t2, Func<T3, T> match_t3, Func<T4, T> match_t4, Func<System.Exception, T> error)
                    => type switch
                    {
                        1 => match_t1(t1),
                        2 => match_t2(t2),
                        3 => match_t3(t3),
                        4 => match_t4(t4),
                        _ => error(exception == null ? new System.NullReferenceException() : exception),
                    };

        public static implicit operator Result<T1, T2, T3, T4>(T1 t1) => new Result<T1, T2, T3, T4> { type = 1, t1 = t1 };
        public static implicit operator Result<T1, T2, T3, T4>(T2 t2) => new Result<T1, T2, T3, T4> { type = 2, t2 = t2 };
        public static implicit operator Result<T1, T2, T3, T4>(T3 t3) => new Result<T1, T2, T3, T4> { type = 3, t3 = t3 };
        public static implicit operator Result<T1, T2, T3, T4>(T4 t4) => new Result<T1, T2, T3, T4> { type = 4, t4 = t4 };
        public static implicit operator Result<T1, T2, T3, T4>(System.Exception exception) => new Result<T1, T2, T3, T4> { exception = exception };
    }
    //Generated
}