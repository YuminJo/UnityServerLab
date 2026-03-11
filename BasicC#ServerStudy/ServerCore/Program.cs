using System;
using System.Threading;
using System.Threading.Tasks;

namespace ServerCore
{
    // 메모리 베리어 (Memory Barrier)
    // CPU나 컴파일러는 성능 최적화를 위해 코드 순서를 바꿀 수 있다.
    // 싱글 스레드에서는 문제가 없지만 멀티 스레드에서는 다른 스레드가 예상과 다른 값을 볼 수 있다.
    // MemoryBarrier는 이런 "명령어 재배치(Reordering)"를 막아주는 역할을 한다.

    // Memory Barrier 종류
    // 1) Full Memory Barrier  : Load / Store 둘 다 순서 변경 금지 (C# Thread.MemoryBarrier)
    // 2) Store Memory Barrier : 쓰기(Store) 순서 변경만 금지
    // 3) Load Memory Barrier  : 읽기(Load) 순서 변경만 금지

    // 쓴다음에 물을 내린다
    // 그래서 _sample = 123; -> 메모리 베리어 _success = true; -> 메모리 베리어 두번
    // 코드 재배치를 막고 가시성도 챙기고

    class Program
    {
        // volatile
        // CPU 캐시가 아니라 항상 메모리에서 값을 읽도록 강제
        // 멀티스레드 환경에서 값의 가시성(visibility)을 보장하기 위해 사용
        static volatile int x = 0;
        static volatile int y = 0;
        static volatile int r1 = 0;
        static volatile int r2 = 0;

        static void Thread_1()
        {
            // y에 1을 저장 (Store)
            y = 1;

            // MemoryBarrier
            // 이 위의 코드와 아래 코드의 실행 순서를 CPU가 바꾸지 못하게 막는다
            // 즉, 반드시 y = 1 이후에 아래 코드가 실행된다
            Thread.MemoryBarrier();

            // x 값을 읽어서 r1에 저장 (Load)
            r1 = x;
        }

        static void Thread_2()
        {
            // x에 1을 저장 (Store)
            x = 1;

            // MemoryBarrier
            // 위의 Store와 아래 Load의 순서를 강제로 유지한다
            Thread.MemoryBarrier();

            // y 값을 읽어서 r2에 저장 (Load)
            r2 = y;
        }

        static void Main(string[] args)
        {
            int count = 0;

            while (true)
            {
                count++;

                // 매 루프마다 변수 초기화
                x = y = r1 = r2 = 0;

                // 두 개의 스레드 실행
                Task t1 = new Task(Thread_1);
                Task t2 = new Task(Thread_2);

                t1.Start();
                t2.Start();

                // 두 스레드가 끝날 때까지 대기
                Task.WaitAll(t1, t2);

                // 이론적으로는 r1과 r2가 동시에 0이 될 수 없다.
                //
                // 하지만 CPU는 성능 최적화를 위해
                // 서로 의존성이 없는 명령어 순서를 바꿀 수 있다.
                //
                // 예)
                // Thread1
                // y = 1
                // r1 = x
                //
                // CPU가 이렇게 바꿀 수도 있다
                //
                // r1 = x
                // y = 1
                //
                // Thread2도 동일하게 순서가 바뀔 수 있다.
                //
                // 그러면 실행 순서가 이렇게 될 수도 있다
                //
                // Thread1 : r1 = x (0 읽음)
                // Thread2 : r2 = y (0 읽음)
                // Thread1 : y = 1
                // Thread2 : x = 1
                //
                // 결과
                // r1 = 0
                // r2 = 0
                //
                // 이런 현상을 "명령어 재배치 (Instruction Reordering)" 라고 한다.

                if (r1 == 0 && r2 == 0)
                    break;
            }

            Console.WriteLine($"{count}번만에 빠져나옴!");
        }
    }
}
