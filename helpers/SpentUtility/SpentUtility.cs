using KEOPBackend.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace KEOPBackend.helpers.SpentUtility
{
    public class SpentUtility
    {
        private readonly UsersDbContext _usersDbContext;
        private readonly SpentAnalysisDbContext _spentAnalysisDbContext;
        public SpentUtility(UsersDbContext usersDbContext, SpentAnalysisDbContext spentAnalysisDbContext)
        {
            _usersDbContext = usersDbContext;
            _spentAnalysisDbContext = spentAnalysisDbContext;

        }

        public async Task<string> CreateJson(int userId)
        {
            string currentMonthYear = DateTime.Now.ToString("MMMM-yyyy");
            var salary = await _usersDbContext.Users.Where(u => u.user_id == userId).Select(u => u.salary).FirstOrDefaultAsync();
            var data = new Dictionary<string, Dictionary<string, int>>
            {
                {
                    currentMonthYear, new Dictionary<string, int>
                    {
                        { "PG", 0 },
                        { "Food", 0 },
                        { "Bills", 0 },
                        { "Traveling", 0 },
                        { "Shopping", 0 },
                        { "Other", 0 },
                        { "Savings", salary }
                    }
                }
            };
            return JsonConvert.SerializeObject(data);
        }

        public async Task<Dictionary<string, Dictionary<string, int>>> JsonForCurrentMonth(int userId, Dictionary<string, Dictionary<string, int>> data)
        {
            string currentMonthYear = DateTime.Now.ToString("MMMM-yyyy");
            var salary = await _usersDbContext.Users.Where(u => u.user_id == userId).Select(u => u.salary).FirstOrDefaultAsync();
            data[currentMonthYear] = new Dictionary<string, int>
            {
                { "PG", 0 },
                { "Food", 0 },
                { "Bills", 0 },
                { "Traveling", 0 },
                { "Shopping", 0 },
                { "Other", 0 },
                { "Savings", salary }
            };

            return data;
        }

        /*public async Task<string> UpdateSpentRecordData(int userId, string spentType, int amount)
        {
            string currentMonthYear = DateTime.Now.ToString("MMMM-yyyy");
            var completeRecord = await _spentAnalysisDbContext.SpentAnalyses
                .Where(s => s.user_id == userId)
                .Select(s => s.spent_data)
                .FirstOrDefaultAsync();

            var completeRecordDict = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, int>>>(completeRecord);

            if (completeRecordDict == null)
            {
                completeRecordDict = new Dictionary<string, Dictionary<string, int>>();
            }

            if (!completeRecordDict.TryGetValue(currentMonthYear, out var currentMonthRecord))
            {
                var salary = await _usersDbContext.Users.Where(u => u.user_id == userId).Select(u => u.salary).FirstOrDefaultAsync();
                currentMonthRecord = new Dictionary<string, int>
                {
                    { "PG", 0 },
                    { "Food", 0 },
                    { "Bills", 0 },
                    { "Traveling", 0 },
                    { "Shopping", 0 },
                    { "Other", 0 },
                    { "Savings", salary }
                };
                completeRecordDict[currentMonthYear] = currentMonthRecord;
            }

            currentMonthRecord[spentType] += amount;
            currentMonthRecord["Savings"] -= amount;

            return JsonConvert.SerializeObject(completeRecordDict);
        }*/

    }
}
