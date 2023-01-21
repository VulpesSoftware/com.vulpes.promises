using System;

namespace Vulpes.Promises
{
    public static class PromiseHelpers
    {
        public static IPromise<Tuple<T1, T2>> All<T1, T2>(IPromise<T1> p1, IPromise<T2> p2)
        {
            T1 val1 = default;
            T2 val2 = default;
            int numUnresolved = 2;
            bool alreadyRejected = false;
            IPromise<Tuple<T1, T2>> promise = Promise<Tuple<T1, T2>>.Create();

            p1.Then(val =>
                {
                    val1 = val;
                    numUnresolved--;
                    if (numUnresolved <= 0)
                    {
                        promise.Resolve(Tuple.Create(val1, val2));
                    }
                })
                .Catch(e =>
                {
                    if (!alreadyRejected)
                    {
                        promise.Reject(e);
                    }

                    alreadyRejected = true;
                })
                .Done();

            p2.Then(val =>
                {
                    val2 = val;
                    numUnresolved--;
                    if (numUnresolved <= 0)
                    {
                        promise.Resolve(Tuple.Create(val1, val2));
                    }
                })
                .Catch(e =>
                {
                    if (!alreadyRejected)
                    {
                        promise.Reject(e);
                    }
                    alreadyRejected = true;
                })
                .Done();

            return promise;
        }

        public static IPromise<Tuple<T1, T2, T3>> All<T1, T2, T3>(IPromise<T1> p1, IPromise<T2> p2, IPromise<T3> p3)
            => All(All(p1, p2), p3).Then(vals => Tuple.Create(vals.Item1.Item1, vals.Item1.Item2, vals.Item2));

        public static IPromise<Tuple<T1, T2, T3, T4>> All<T1, T2, T3, T4>(IPromise<T1> p1, IPromise<T2> p2, IPromise<T3> p3, IPromise<T4> p4)
            => All(All(p1, p2), All(p3, p4)).Then(vals => Tuple.Create(vals.Item1.Item1, vals.Item1.Item2, vals.Item2.Item1, vals.Item2.Item2));
    }
}