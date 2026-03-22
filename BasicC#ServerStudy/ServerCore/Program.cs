using System;
using System.Threading;
using System.Threading.Tasks;

namespace ServerCore
{
    // =====================================================================
    //  데드락(DeadLock) 발생 조건 4가지
    //  1. 상호 배제 (Mutual Exclusion)  - lock 으로 자원 독점
    //  2. 점유 대기 (Hold & Wait)       - lock 을 쥔 채 다른 lock 대기
    //  3. 비선점   (No Preemption)      - 강제로 lock 을 빼앗을 수 없음
    //  4. 순환 대기 (Circular Wait)     - A→B→A 순환 구조
    // =====================================================================

    // -----------------------------------------------------------------------
    //  [케이스 1]  SessionManager ↔ UserManager 교차 락 → 데드락
    //
    //  Thread-1: SessionManager._lock 획득 → UserManager._lock 시도
    //  Thread-2: UserManager._lock    획득 → SessionManager._lock 시도
    //  → 서로 상대방의 락을 기다리며 영원히 멈춤
    // -----------------------------------------------------------------------
    class SessionManager
    {
        static object _lock = new object();

        public static void TestSession()
        {
            lock (_lock)
            {
                Console.WriteLine("[SessionManager] lock 획득 - TestSession");
                Thread.Sleep(100); // 컨텍스트 스위치 유도
                // 여기서 UserManager._lock 을 기다림
            }
        }

        // Thread-1 진입점
        public static void Test()
        {
            lock (_lock) // (1) SessionManager._lock 획득
            {
                Console.WriteLine("[SessionManager.Test] SessionManager lock 획득");
                Thread.Sleep(100); // Thread-2 가 UserManager._lock 을 잡을 시간 확보
                Console.WriteLine("[SessionManager.Test] UserManager.TestUser 호출 시도...");
                UserManager.TestUser(); // (2) UserManager._lock 시도 → 데드락!
            }
        }
    }

    class UserManager
    {
        static object _lock = new object();

        // Thread-2 진입점
        public static void Test()
        {
            lock (_lock) // (1) UserManager._lock 획득
            {
                Console.WriteLine("[UserManager.Test] UserManager lock 획득");
                Thread.Sleep(100); // Thread-1 이 SessionManager._lock 을 잡을 시간 확보
                Console.WriteLine("[UserManager.Test] SessionManager.TestSession 호출 시도...");
                SessionManager.TestSession(); // (2) SessionManager._lock 시도 → 데드락!
            }
        }

        public static void TestUser()
        {
            lock (_lock)
            {
                Console.WriteLine("[UserManager] lock 획득 - TestUser");
            }
        }
    }

    // -----------------------------------------------------------------------
    //  [케이스 2]  Monitor.Enter 후 예외 발생 → 락 미해제 → 데드락
    //
    //  Monitor.Enter / Monitor.Exit 를 직접 쓸 때
    //  중간에 예외가 터지면 Exit 가 호출되지 않아 락이 영구 점유됨.
    //  → lock(){ } 또는 try/finally 로 반드시 감싸야 한다.
    // -----------------------------------------------------------------------
    class MonitorDeadLock
    {
        static object _obj = new object();

        public static void Run()
        {
            // 스레드 A : 락을 잡은 채 예외 발생 → Exit 미호출
            Thread threadA = new Thread(() =>
            {
                Monitor.Enter(_obj);
                Console.WriteLine("[Monitor케이스] Thread-A: lock 획득");
                try
                {
                    throw new Exception("의도적 예외 - Monitor.Exit 가 호출되지 않음!");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"[Monitor케이스] Thread-A 예외: {e.Message}");
                    // ※ Monitor.Exit(_obj) 를 호출하지 않고 빠져나감
                    //    수정하려면 finally { Monitor.Exit(_obj); } 추가
                }
            });

            // 스레드 B : 같은 락을 기다리다가 영원히 블로킹
            Thread threadB = new Thread(() =>
            {
                Console.WriteLine("[Monitor케이스] Thread-B: lock 획득 대기 중...");
                // Thread-A 가 Exit 를 안 불렀으므로 여기서 무한 대기
                bool acquired = Monitor.TryEnter(_obj, TimeSpan.FromSeconds(3));
                if (acquired)
                {
                    Console.WriteLine("[Monitor케이스] Thread-B: lock 획득 성공 (비정상 케이스)");
                    Monitor.Exit(_obj);
                }
                else
                {
                    Console.WriteLine("[Monitor케이스] Thread-B: ★ 3초 후 타임아웃 - 데드락 확인!");
                }
            });

            threadA.Start();
            threadA.Join();   // A 가 Exit 없이 종료된 뒤
            threadB.Start();
            threadB.Join();
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("======================================");
            Console.WriteLine("  케이스 2 먼저: Monitor 락 미해제");
            Console.WriteLine("======================================");
            MonitorDeadLock.Run();

            Console.WriteLine();
            Console.WriteLine("======================================");
            Console.WriteLine("  케이스 1: 교차 락 데드락 시작");
            Console.WriteLine("  (아래 출력 후 프로그램이 멈춥니다)");
            Console.WriteLine("======================================");

            // Thread-1: SessionManager.Test() → SessionManager._lock → UserManager._lock 시도
            Task t1 = new Task(SessionManager.Test);
            // Thread-2: UserManager.Test()    → UserManager._lock    → SessionManager._lock 시도
            Task t2 = new Task(UserManager.Test);

            t1.Start();
            t2.Start();

            // 5초 후에도 안 끝나면 데드락으로 간주
            bool finished = Task.WaitAll(new[] { t1, t2 }, TimeSpan.FromSeconds(5));

            if (!finished)
                Console.WriteLine("\n★★★ 데드락 발생! 5초 안에 완료되지 않음 ★★★");
            else
                Console.WriteLine("\n정상 완료 (데드락 미발생)");

            Console.WriteLine("\n[해결책]");
            Console.WriteLine("1. 락 획득 순서를 모든 스레드에서 동일하게 통일한다.");
            Console.WriteLine("2. Monitor.TryEnter + timeout 으로 대기 시간을 제한한다.");
            Console.WriteLine("3. lock 대신 SemaphoreSlim / ReaderWriterLockSlim 등 고수준 동기화 사용.");
            Console.WriteLine("4. Monitor.Enter 는 반드시 try/finally { Monitor.Exit } 로 감싼다.");
        }
    }
}
