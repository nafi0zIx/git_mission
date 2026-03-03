using System.Text.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MiniDataCenter
{
    class Participant
    {
        public string SteamId { get; set; } = "";
        public string DiscordId { get; set; } = "";
        public int Warnings { get; set; }
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
        }

        public IReadOnlyList<Participant> GetAll() => _items.AsReadOnly();

        public bool Add(Participant p)
        {
            if (string.IsNullOrWhiteSpace(p.SteamId)) return false;
            if (_items.Exists(x => x.SteamId == p.SteamId)) return false;
            _items.Add(p);
            Save();
            return true;
        }

        public bool RemoveBySteamId(string steamId)
        {
            var idx = _items.FindIndex(x => x.SteamId == steamId);
            if (idx < 0) return false;
            _items.RemoveAt(idx);
            Save();
            return true;
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
                Console.WriteLine("Мини ДЦ — команды:");
                Console.WriteLine("1 — Просмотреть всех");
                Console.WriteLine("2 — Добавить участника");
                Console.WriteLine("3 — Удалить участника (по Steam ID)");
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
                        Console.WriteLine($"Steam ID:   {p.SteamId}");
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

                    Console.Write("Discord ID: ");
                    p.DiscordId = Console.ReadLine()?.Trim() ?? "";

                    Console.Write("Кол-во предупреждений (число): ");
                    var warnStr = Console.ReadLine()?.Trim() ?? "0";
                    if (!int.TryParse(warnStr, out var warns)) warns = 0;
                    p.Warnings = warns;

                    p.JoinedAt = DateTime.Now;

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
                else
                {
                    Console.WriteLine("Неизвестная команда.");
                }
            }

            Console.WriteLine("Выход. Пока.");
        }
    }
}
