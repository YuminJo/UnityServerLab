using System;
using System.Threading;
using System.Threading.Tasks;

namespace ServerCore
{
    class Program
    {
        static void MainThread(object state)
        {
            Console.WriteLine("Hello Thread!");
        }

        static void Main(string[] args)
        {
            // ThreadPool 설정
            // 첫 번째 인자: Worker Thread 개수
            // 최소 1개, 최대 5개로 설정
            ThreadPool.SetMinThreads(1, 1);
            ThreadPool.SetMaxThreads(5, 5);

            for (int i = 0; i < 5; i++)
            {
                // Task를 직접 생성해서 작업 단위를 정의
                // LongRunning 옵션:
                // 오래 걸리는 작업이라고 알려주면 ThreadPool 대신
                // 새로운 전용 Thread를 생성해서 실행한다.
                // (ThreadPool을 점유하지 않도록 하기 위함)
                Task t = new Task(() => { while (true) { } }, TaskCreationOptions.LongRunning);
                t.Start();
            }

            // 아래 코드는 ThreadPool을 직접 사용하는 예시
            // ThreadPool의 Worker Thread가 작업을 처리한다
            // 하지만 무한 루프 같은 작업을 넣으면
            // ThreadPool의 스레드가 반환되지 않아 문제가 생길 수 있다.

            //for (int i = 0; i < 4; i++)
            //    ThreadPool.QueueUserWorkItem((obj) => { while (true) { } });

            // ThreadPool에 작업을 요청
            // 대기 중인 Worker Thread가 있으면 해당 작업을 실행한다.
            ThreadPool.QueueUserWorkItem(MainThread!);

            while (true) { }

            //// 스레드를 직접 생성하는 방법
            //// 스레드는 생성 비용이 크기 때문에
            //// CPU 코어 수에 맞게 사용하는 것이 중요하다.
            //// ThreadPool은 이런 비용을 줄이기 위해 미리 스레드를 만들어 두고 재사용한다.

            //Thread t = new Thread(MainThread);
            //t.Name = "Test Thread";

            //// Background Thread로 설정하면
            //// Main Thread가 종료될 때 함께 종료된다.
            //// t.IsBackground = true;

            //t.Start();

            //Console.WriteLine("Waiting for Thread!");

            //// Join: 해당 스레드가 종료될 때까지 대기
            //t.Join();

            //Console.WriteLine("Hello World!");
        }
    }
}
