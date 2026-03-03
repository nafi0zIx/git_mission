using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace MiniDataCenter
{
    class Participant
    {
        public int Index { get; set; }             // автоматически присваивается репозиторием
        public string SteamId { get; set; } = "";
        public string DiscordId { get; set; } = "";
        public string Nickname { get; set; } = "";
        public int Warnings { get; set; } = 0;     // по умолчанию 0
        public DateTime JoinedAt { get; set; }
        public string Socials { get; set; } = "";
    }

    class Repository
    {
        private readonly string _filePath;
        private List<Participant> _items = new();

        public Repository(string filePath)
        {
            _filePath = filePath;
            Load();
            Reindex();
        }

        public IReadOnlyList<Participant> GetAll() => _items.AsReadOnly();

        public bool Add(Participant p)
        {
            if (string.IsNullOrWhiteSpace(p.SteamId)) return false;
            if (_items.Exists(x => x.SteamId == p.SteamId)) return false;
            p.JoinedAt = DateTime.Now;
            _items.Add(p);
            Reindex();
            Save();
            return true;
        }

        public bool RemoveBySteamId(string steamId)
        {
            var idx = _items.FindIndex(x => x.SteamId == steamId);
            if (idx < 0) return false;
            _items.RemoveAt(idx);
            Reindex();
            Save();
            return true;
        }

        public Participant? FindBySteamId(string steamId) =>
            _items.FirstOrDefault(x => x.SteamId == steamId);

        public Participant? FindByIndex(int index) =>
            _items.FirstOrDefault(x => x.Index == index);

        public bool IncrementWarningsBySteamId(string steamId, int delta = 1)
        {
            var p = FindBySteamId(steamId);
            if (p == null) return false;
            p.Warnings = Math.Max(0, p.Warnings + delta);
            Save();
            return true;
        }

        public bool IncrementWarningsByIndex(int index, int delta = 1)
        {
            var p = FindByIndex(index);
            if (p == null) return false;
            p.Warnings = Math.Max(0, p.Warnings + delta);
            Save();
            return true;
        }

        public bool ExportToText(string outPath)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("Список участников");
                sb.AppendLine($"Экспортировано: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine(new string('-', 60));

                foreach (var p in _items)
                {
                    sb.AppendLine($"Index:      {p.Index}");
                    sb.AppendLine($"Steam ID:   {p.SteamId}");
                    sb.AppendLine($"Nickname:   {p.Nickname}");
                    sb.AppendLine($"Discord ID: {p.DiscordId}");
                    sb.AppendLine($"Warnings:   {p.Warnings}");
                    sb.AppendLine($"Joined At:  {p.JoinedAt:yyyy-MM-dd HH:mm:ss}");
                    sb.AppendLine($"Socials:    {p.Socials}");
                    sb.AppendLine(new string('-', 60));
                }

                File.WriteAllText(outPath, sb.ToString(), Encoding.UTF8);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void Reindex()
        {
            for (int i = 0; i < _items.Count; i++)
                _items[i].Index = i + 1; // нумерация с 1
        }

        private void Load()
        {
            try
            {
                if (!File.Exists(_filePath))
                {
                    _items = new List<Participant>();
                    return;
                }

                var json = File.ReadAllText(_filePath);
                var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                _items = JsonSerializer.Deserialize<List<Participant>>(json, opts) ?? new List<Participant>();
            }
            catch
            {
                _items = new List<Participant>();
            }
        }

        private void Save()
        {
            try
            {
                var opts = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(_items, opts);
                File.WriteAllText(_filePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка сохранения: {ex.Message}");
            }
        }
    }

    class Program
    {
        static void Main()
        {
            const string dbFile = "participants.json";
            var repo = new Repository(dbFile);

            while (true)
            {
                Console.WriteLine();
                Console.WriteLine("Дата-Центр Контент-Мейкеров — команды:");
                Console.WriteLine("1 — Просмотреть всех");
                Console.WriteLine("2 — Добавить участника");
                Console.WriteLine("3 — Удалить участника (по Steam ID)");
                Console.WriteLine("4 — Добавить предупреждение (по Steam ID или Index)");
                Console.WriteLine("5 — Экспорт в текстовый файл");
                Console.WriteLine("0 — Выход");
                Console.Write("Выберите команду: ");
                var cmd = Console.ReadLine()?.Trim();

                if (cmd == "0") break;

                if (cmd == "1")
                {
                    var list = repo.GetAll();
                    if (list.Count == 0)
                    {
                        Console.WriteLine("Список пуст.");
                        continue;
                    }

                    Console.WriteLine();
                    Console.WriteLine("Список участников:");
                    foreach (var p in list)
                    {
                        Console.WriteLine("--------------------------------------------------");
                        Console.WriteLine($"Index:      {p.Index}");
                        Console.WriteLine($"Steam ID:   {p.SteamId}");
                        Console.WriteLine($"Nickname:   {p.Nickname}");
                        Console.WriteLine($"Discord ID: {p.DiscordId}");
                        Console.WriteLine($"Warnings:   {p.Warnings}");
                        Console.WriteLine($"Joined At:  {p.JoinedAt:yyyy-MM-dd HH:mm:ss}");
                        Console.WriteLine($"Socials:    {p.Socials}");
                    }
                    Console.WriteLine("--------------------------------------------------");
                }
                else if (cmd == "2")
                {
                    var p = new Participant();

                    Console.Write("Steam ID: ");
                    p.SteamId = Console.ReadLine()?.Trim() ?? "";

                    Console.Write("Nickname: ");
                    p.Nickname = Console.ReadLine()?.Trim() ?? "";

                    Console.Write("Discord ID: ");
                    p.DiscordId = Console.ReadLine()?.Trim() ?? "";


                    Console.Write("Socials (одна строка): ");
                    p.Socials = Console.ReadLine()?.Trim() ?? "";

                    var ok = repo.Add(p);
                    Console.WriteLine(ok ? "Участник добавлен." : "Не удалось добавить (возможно, пустой или дубликат Steam ID).");
                }
                else if (cmd == "3")
                {
                    Console.Write("Укажите Steam ID для удаления: ");
                    var steamId = Console.ReadLine()?.Trim() ?? "";
                    if (string.IsNullOrEmpty(steamId))
                    {
                        Console.WriteLine("Пустой Steam ID — отмена.");
                        continue;
                    }

                    var removed = repo.RemoveBySteamId(steamId);
                    Console.WriteLine(removed ? "Участник удалён." : "Участник с таким Steam ID не найден.");
                }
                else if (cmd == "4")
                {
                    Console.WriteLine("Выберите способ: 1 — по Steam ID, 2 — по Index");
                    var way = Console.ReadLine()?.Trim();
                    if (way == "1")
                    {
                        Console.Write("Steam ID: ");
                        var sid = Console.ReadLine()?.Trim() ?? "";
                        if (string.IsNullOrEmpty(sid)) { Console.WriteLine("Пустой Steam ID — отмена."); continue; }

                        var ok = repo.IncrementWarningsBySteamId(sid, 1);
                        Console.WriteLine(ok ? "Предупреждение добавлено." : "Участник с таким Steam ID не найден.");
                    }
                    else if (way == "2")
                    {
                        Console.Write("Index: ");
                        var idxStr = Console.ReadLine()?.Trim() ?? "";
                        if (!int.TryParse(idxStr, out var idx)) { Console.WriteLine("Неверный индекс."); continue; }

                        var ok = repo.IncrementWarningsByIndex(idx, 1);
                        Console.WriteLine(ok ? "Предупреждение добавлено." : "Участник с таким индексом не найден.");
                    }
                    else
                    {
                        Console.WriteLine("Неверный выбор.");
                    }
                }
                else if (cmd == "5")
                {
                    Console.Write("Имя выходного файла (например export.txt): ");
                    var outPath = Console.ReadLine()?.Trim();
                    if (string.IsNullOrEmpty(outPath)) outPath = "export.txt";

                    var ok = repo.ExportToText(outPath);
                    Console.WriteLine(ok ? $"Экспорт сохранён в {outPath}." : "Ошибка экспорта.");
                }
                else
                {
                    Console.WriteLine("Неизвестная команда.");
                }
            }

            Console.WriteLine("Выход.");
        }
    }
}
