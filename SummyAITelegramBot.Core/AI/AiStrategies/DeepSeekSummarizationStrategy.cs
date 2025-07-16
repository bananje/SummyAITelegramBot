using SummyAITelegramBot.Core.AI.Abstractions;
using System.Text.Json;
using System.Text;
using SummyAITelegramBot.Core.AI.Attributes;
using System.Text.RegularExpressions;

namespace SummyAITelegramBot.Core.AI.AiStrategies;

[SummarizationStrategy("DeepSeek")]
public class DeepSeekSummarizationStrategy : ISummarizationStrategy
{
    private readonly HttpClient _client;
    public DeepSeekSummarizationStrategy(IHttpClientFactory factory)
    {
        _client = factory.CreateClient("DeepSeek");
    }

    public async Task<string> SummarizeAsync(string inputText)
    {
        var contextRole = "Ты — профессиональный редактор новостей. ";
        var promt = "Ты — профессиональный редактор новостей. " +
            "Сократи текст ниже до ОДНОГО предложения (максимум 15 слов), сохранив:  " +
            "\r\n1. Ключевое событие/действие  \r\n2. Основного субъекта (компания/человек)  " +
            "\r\n3. Важные цифры/даты (если есть)  \r\n4. Неожиданные или критичные последствия (если упомянуты)" +
            "  \r\nИгнорируй: мнения автора, повторы, детали без прямого отношения к сути.";

        var request = new
        {
            model = "deepseek/deepseek-r1-0528-qwen3-8b:free",
            messages = new[]
                {
                    new { role = "system", content = contextRole },
                    new { role = "user", content = $"{promt} Сделай короткую сводку следующего текста: {inputText}" }
                }
        };

        var content = new StringContent(
                JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("chat/completions", content);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        return doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? "[OpenRouter: пустой ответ]";
    }

    public async Task<bool> ValidateOfUniqueTextAsync(string allTexts, string currentText)
    {
        var contextRole = "Ты — ассистент, проверяющий уникальность постов. " +
            "Отвечай строго: true или false. Без объяснений.";
        var prompt = $"""
            У тебя есть список предыдущих постов и новый пост. 
            Ответь только "true" (если новый пост по смыслу уникален) 
            или "false" (если он дублирует или почти повторяет предыдущие посты).

            Предыдущие посты:
            ---
            {allTexts}
            ---

            Новый пост:
            ---
            {currentText}
            ---

            Ответь строго: true или false. Без пояснений.
        """;

        var request = new
        {
            model = "deepseek/deepseek-r1-0528-qwen3-8b:free",
            messages = new[]
            {
            new { role = "system", content = contextRole},
            new { role = "user", content = prompt }
        }
        };

        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("chat/completions", content);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        var answerRaw = doc.RootElement
        .GetProperty("choices")[0]
        .GetProperty("message")
        .GetProperty("content")
        .GetString();

        var match = Regex.Match(answerRaw ?? "", @"\b(true|false)\b", RegexOptions.IgnoreCase);
        if (!match.Success)
            throw new Exception($"Невалидный ответ от модели: {answerRaw}");

        var answer = match.Value.ToLowerInvariant();

        return answer == "true";

        // Лог или выброс исключения, если ответ невалидный
        throw new Exception($"Невалидный ответ от модели: {answerRaw}");
    }

    public async Task<bool> СheckForAdvertising(string inputText)
    {
        var prompt = $"""
            Роль: Ты — профессиональный Content Curator (Куратор Контента) для Telegram-бота, создающий краткие, информативные сводки из множества постов в каналах пользователя. Твоя ключевая задача — беспощадно удалять рекламный и коммерческий контент, сохраняя только полезную, новостную, образовательную или развлекательную информацию для итоговой сводки.

        Задача: Проанализируй предоставленный текст поста из Telegram-канала и прими решение: ВКЛЮЧАТЬ его в общую сводку или ИСКЛЮЧИТЬ как рекламу/коммерцию.

        Строгие Критерии Исключения (Реклама - ИСКЛЮЧИТЬ!):
        1.  Прямые призывы к покупке/продаже: Слова: "купи", "продам", "закажи", "оформить", "доставка", "акция", "скидка", "распродажа", "выгодно", "только сейчас", "специальное предложение", "предзаказ".
        2.  Продвижение товаров/услуг/брендов: Упоминание конкретных брендов (кроме контекста новостей), моделей товаров, названий компаний с целью их рекламы. Описание преимуществ продукта/услуги.
        3.  Партнерские ссылки и промокоды: Любые ссылки с реферальными метками (ref=, ?utm_source=partner), промокоды ("используй код WORD20"), приглашения в другие каналы/боты/чаты с коммерческим уклоном.
        4.  Предложения услуг/работ: "Предлагаю услуги...", "Ищу сотрудников...", "Фриланс...", "Сделаю на заказ...".
        5.  Очевидные признаки рекламного поста: Чрезмерное использование эмодзи (особенно ➡️, ✅, 🔥, 💰), шаблонные рекламные фразы ("Не упусти шанс!", "Лучшее предложение месяца!"), призывы перейти по ссылке в шапку профиля/био.
        6.  Псевдоновости с коммерческой целью: Посты, маскирующиеся под новости, но основной целью которых является продвижение продукта/услуги/мероприятия с платным входом.

        Критерии Включения (Полезный Контент - ВКЛЮЧИТЬ!):
        1.  Информативные новости: Анонсы событий, обзоры, аналитика, важные объявления (без коммерции), обсуждения трендов.
        2.  Образовательный/Познавательный контент: Инструкции, лайфхаки, объяснения, интересные факты.
        3.  Объективные обзоры: Критические мнения о продуктах/сервисах, сравнения, *если* цель — информирование, а не прямая продажа.
        4.  Культура/Развлечения: Анонсы культурных мероприятий (если не платная реклама билетов), мемы, интересные истории, дискуссии.
        5.  Упоминания брендов в нейтральном/новостном контексте: "Компания X представила новый процессор" (новость) - ВКЛЮЧИТЬ. "Купите потрясающий новый процессор от X по скидке!" - ИСКЛЮЧИТЬ.

        Алгоритм Действий:
        1.  Внимательно прочти текст поста.
        2.  Ищи ЛЮБЫЕ признаки из списка "Критерии Исключения".
        3.  Спроси себя:
            *   Есть ли здесь прямой или косвенный призыв к трате денег, регистрации или переходу по ссылке с коммерческой целью?
            *   Является ли основная цель этого поста — продвижение конкретного платного продукта, услуги, бренда или канала?
            *   Содержит ли пост партнерские ссылки, промокоды или реферальные призывы?
        4.  Прими решение:
            *   Если обнаружен ХОТЯ БЫ ОДИН четкий признак рекламы/коммерции (из Критериев Исключения 1-6) -> Пост = РЕКЛАМА -> ИСКЛЮЧИТЬ из сводки. Не суммируй его.
            *   Если пост НЕ содержит признаков рекламы и соответствует Критериям Включения 1-5 -> Пост = ПОЛЕЗНЫЙ КОНТЕНТ -> ВКЛЮЧИТЬ его краткое изложение в сводку.
        5.  Если сомневаешься (пост на грани) -> Скорее всего, это реклама. ИСКЛЮЧАЙ. Лучше пропустить потенциально полезный пост, чем допустить рекламу в сводку. Приоритет - АНТИ-СПАМ.

        Важно:
        *   Будь максимально строгим. Малейший намек на рекламу = исключение.
        *   Не пытайся "исправить" рекламный пост или удалить из него только рекламные фразы. Весь пост исключается целиком.
        *   Фокус на намерении: Главное — коммерческое *намерение* поста.
        *   Контекст: Учитывай общий контекст канала, но не давай рекламе "спасательный круг" из-за него. Даже в канале про скидки пост со скидкой — это реклама.

        Твой вывод ДОЛЖЕН быть только одним словом:
        *   true- если пост БЕЗ рекламы и подходит для сводки.
        *   false - если пост содержит ЛЮБЫЕ признаки рекламы/коммерции.
        """;

        var request = new
        {
            model = "deepseek/deepseek-r1-0528-qwen3-8b:free",
            messages = new[]
            {
            new { role = "system", content = prompt },
            new { role = "user", content = $"{prompt}. \n Вот текст поста: {inputText}" }
        }
        };

        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("chat/completions", content);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        var answerRaw = doc.RootElement
        .GetProperty("choices")[0]
        .GetProperty("message")
        .GetProperty("content")
        .GetString();

        var match = Regex.Match(answerRaw ?? "", @"\b(true|false)\b", RegexOptions.IgnoreCase);
        if (!match.Success)
            throw new Exception($"Невалидный ответ от модели: {answerRaw}");

        var answer = match.Value.ToLowerInvariant();

        return answer == "true";

        // Лог или выброс исключения, если ответ невалидный
        throw new Exception($"Невалидный ответ от модели: {answerRaw}");
    }
}