using System;
using System.Threading;
using System.Threading.Tasks;

namespace ServerCore
{
    class Program
    {
        static int number = 0;
        static object _obj = new object();

        // Enter 와 Exit를 쓰면 관리하기가 어려워진다
        // 문을 잠구고 return하면 출입이 안된다 그걸 데드락 DeadLock 죽은 자물쇠 라는 뜻
        static void Thread_1()
        {
            // 상호베제 Mutual Exclusive
            Monitor.Enter(_obj); // 문을 잠구는 행위

            // atomic = 원자성
            // 더이상 쪼개지면 안되는 그러한 작업
            // 어떤 동작을 한번에 일어나야 한다
            // 원자적으로 덧셈을 한다 원자적으로 뺄셈을 한다
            // interlocked를 사용하면 volalite 사용안해도 된다.

            for (int i = 0; i < 100000; i++)
            {
                number++;
                // All or Nothing
                //int afterValue = Interlocked.Increment(ref number); // 1
            }

            Monitor.Exit(_obj); // 잠금을 풀어준다

            // 좀더 편리하게
            for (int i = 0; i < 100000; i++)
            {
                // 상호베제 Mutual Exclusive
                lock (_obj)
                {
                    number++;
                }
                // All or Nothing
                //int afterValue = Interlocked.Increment(ref number); // 1
            }
        }

        static void Thread_2()
        {
            Monitor.Enter(_obj);

            for (int i = 0; i < 100000; i++)
            {
                number--;
                //Interlocked.Decrement(ref number); // number = 1 -> 0
            }

            Monitor.Exit(_obj);
        }

        static void Main(string[] args)
        {
            //number++;
            //number++은 3단계에 걸쳐서 일어나고 있다.
            //int temp = number;
            //temp += 1;
            //number = temp;

            Task t1 = new Task(Thread_1);
            Task t2 = new Task(Thread_2);
            t1.Start();
            t2.Start();

            Task.WaitAll(t1, t2);

            Console.WriteLine(number);
        }
    }
}
