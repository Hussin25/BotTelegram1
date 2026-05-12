using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

class Program
{
    private static readonly string botToken = "8551195330:AAF4euPqw13DZfhGtrRFS38L2Xj-fMe4j3M";
    private static readonly TelegramBotClient botClient = new TelegramBotClient(botToken);

    // تخزين حالة كل مستخدم (لإدارة الجلسات)
    private static Dictionary<long, UserSession> userSessions = new Dictionary<long, UserSession>();

    static async Task Main(string[] args)
    {
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = Array.Empty<UpdateType>() // استمع لكل التحديثات
        };

        botClient.StartReceiving(HandleUpdateAsync, HandleErrorAsync, receiverOptions);

        Console.WriteLine("Bot is running...");
        Console.ReadLine(); // لمنع التطبيق من الإغلاق
    }

    // معالجة التحديثات من Telegram
    public static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, System.Threading.CancellationToken cancellationToken)
    {
        if (update.Type == UpdateType.Message && update.Message.Text != null)
        {
            long userId = update.Message.Chat.Id;
            string userMessage = update.Message.Text.ToLower();

            // إذا كان المستخدم جديدًا، أنشئ جلسة جديدة
            if (!userSessions.ContainsKey(userId))
            {
                userSessions[userId] = new UserSession();
            }

            var session = userSessions[userId];
            string botResponse = "";

            // إذا كان المستخدم في لعبة أو اختبار، تابع الجولة الحالية
            if (session.InGame || session.InQuiz)
            {
                botResponse = HandleSession(userMessage, session);
            }
            else
            {
                botResponse = HandleCommand(userMessage, session, update);
            }

            await botClient.SendMessage(userId, botResponse);
        }
    }

    // التعامل مع الأوامر الأساسية
    public static string HandleCommand(string userMessage, UserSession session, Update update)
    {
        switch (userMessage)
        {
            case "/start":
                Console.WriteLine(update.Message.Chat.FirstName + " ,Command User == " + userMessage);
                return "أهلاً! اختر من القائمة: /game, /quiz, /truefalse";


            case "/game":
                session.StartGame();
                Console.WriteLine(update.Message.Chat.FirstName + " ,Command User == " + userMessage);
                return "لعبة التخمين: حاول تخمين الرقم من 1 إلى 10";

            case "/quiz":
                session.StartQuiz();
                Console.WriteLine(update.Message.Chat.FirstName + " ,Command User == " + userMessage);
                return "سؤال عام: ما هو أكبر كوكب في النظام الشمسي؟ (1) الأرض (2) المريخ (3) المشتري";

            case "/truefalse":
                session.StartTrueFalse();
                Console.WriteLine(update.Message.Chat.FirstName + " ,Command User == " + userMessage);
                return "سؤال صح أو خطأ: الأرض مسطحة؟ (صح/خطأ)";

            default:
                Console.WriteLine(update.Message.Chat.FirstName + " ,Command User == " + userMessage);
                return "لم أفهم ذلك، جرب /start.";
        }
    }

    // إدارة جولات الألعاب أو الأسئلة
    public static string HandleSession(string userMessage, UserSession session)
    {
        string response = "";

        switch (session)
        {
            case var s when s.InGame:
                {
                    if (int.TryParse(userMessage, out int guess))
                    {
                        response = guess == session.CurrentAnswer
                            ? "صحيح! لقد ربحت. اختر /start للعب مرة أخرى."
                            : "خاطئ! حاول مرة أخرى. الرقم الصحيح هو: " + session.CurrentAnswer;
                        Console.WriteLine(response);

                        if (guess == session.CurrentAnswer)
                        {
                            session.EndGame();
                        }
                    }
                    else
                    {
                        response = "الرجاء إدخال رقم.";
                    }
                    break;
                }

            case var s when s.InQuiz:
                {
                    response = userMessage == "3"
                        ? "صحيح! المشتري هو أكبر كوكب."
                        : "خاطئ! الإجابة الصحيحة هي المشتري.";
                    Console.WriteLine(response);
                    session.EndQuiz();
                    break;
                }

            case var s when s.InTrueFalse:
                {
                    response = userMessage == "خطأ"
                        ? "صحيح! الأرض ليست مسطحة."
                        : "خاطئ! الإجابة الصحيحة هي 'خطأ'.";

                    session.EndTrueFalse();
                    break;
                }

            default:
                response = "لم أتمكن من فهم طلبك، حاول مرة أخرى.";
                break;
        }

        return response;

    }

    public static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, System.Threading.CancellationToken cancellationToken)
    {
        Console.WriteLine($"Error: {exception.Message}");
        return Task.CompletedTask;
    }
}

// فئة لتخزين حالة المستخدم وجلساته
class UserSession
{
    public bool InGame { get; private set; }
    public bool InQuiz { get; private set; }
    public bool InTrueFalse { get; private set; }
    public int CurrentAnswer { get; private set; }

    public void StartGame()
    {
        InGame = true;
        CurrentAnswer = new Random().Next(1, 11); // تخمين رقم من 1 إلى 10
    }

    public void StartQuiz()
    {
        InQuiz = true;
    }

    public void StartTrueFalse()
    {
        InTrueFalse = true;
    }

    public void EndGame()
    {
        InGame = false;
    }

    public void EndQuiz()
    {
        InQuiz = false;
    }

    public void EndTrueFalse()
    {
        InTrueFalse = false;
    }
}
