using System;
using System.Threading;
using System.Threading.Tasks;

namespace ServerCore
{
    class Program
    {
        // volatile 키워드
        // - 이 변수는 여러 스레드에서 동시에 접근할 수 있는 변수이다.
        // - CPU 캐시를 사용하지 않고 항상 "메모리에 있는 최신 값"을 읽도록 강제한다.
        // - 컴파일러나 CPU가 최적화하면서 값을 캐시에 저장해버리는 것을 방지한다.
        // - 즉, 다른 스레드가 값을 변경하면 바로 그 변경된 값을 볼 수 있게 된다.
        // - 근데 나중에는 락이라던가 아토믹 같은 다른 옵션을 쓰기에 그냥 있는것만 알아두자.
        volatile static bool _stop = false;

        static void ThreadMain()
        {
            Console.WriteLine("쓰레드 시작");

            // 만약 volatile이 없다면
            // 컴파일러가 "_stop은 어차피 false겠지" 라고 판단해서
            // while(true) 같은 무한 루프로 최적화할 수 있다.
            //
            // volatile을 사용하면 매 반복마다 메모리에서
            // 최신 _stop 값을 다시 읽어온다.

            while (_stop == false)
            {
                // 메인 스레드가 _stop을 true로 바꿔주기를 기다리는 중
            }

            Console.WriteLine("쓰레드 종료");
        }

        static void Main(string[] args)
        {
            // 스레드마다 각각의 Stack 메모리를 따로 사용한다.
            // 하지만 static 변수는 모든 스레드가 공유하는 메모리 영역에 존재한다.
            //
            // 따라서 여러 스레드가 동시에 접근하면
            // 값이 서로 보이지 않거나 동기화 문제가 생길 수 있다.

            Task t = new Task(ThreadMain);
            t.Start();

            // 1초 동안 대기
            Thread.Sleep(1000);

            // 작업 스레드에게 종료 신호 전달
            _stop = true;

            // Release 모드에서 volatile이 없다면
            // 작업 스레드가 _stop 값을 갱신된 값으로 보지 못해서
            // 여기서 무한 대기 상태에 빠질 수 있다.
            Console.WriteLine("Stop 호출");
            Console.WriteLine("종료 대기중");

            // 작업 스레드가 종료될 때까지 기다림
            t.Wait();

            Console.WriteLine("종료 성공");
        }
    }
}
