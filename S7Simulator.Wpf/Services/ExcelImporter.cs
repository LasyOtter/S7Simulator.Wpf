using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using S7Simulator.Wpf.Models;
using S7Simulator.Wpf.Plc;

namespace S7Simulator.Wpf.Services;

public static class ExcelImporter
{
    private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(ExcelImporter));
    
    public static async Task ImportFromExcelAsync(string excelPath, PlcMemory memory, Action<string> statusCallback)
    {
        ExcelPackage.License.SetNonCommercialPersonal("chowjimy");

        if (!File.Exists(excelPath)) throw new FileNotFoundException(excelPath);

        var dbDict = new Dictionary<int, DbInfo>();

        using var package = new ExcelPackage(new FileInfo(excelPath));
        var ws = package.Workbook.Worksheets[0];
        int row = 2;

        while (row <= ws.Dimension.End.Row && !string.IsNullOrEmpty(ws.Cells[row, 1].Text))
        {
            int dbNo = Convert.ToInt32(ws.Cells[row, 1].Value);
            if (!dbDict.TryGetValue(dbNo, out var db))
            {
                db = new DbInfo { DbNumber = dbNo, Name = $"DB{dbNo}" };
                dbDict[dbNo] = db;
            }

            var v = new VariableInfo
            {
                DbNumber = dbNo,
                Name = ws.Cells[row, 2].Text.Trim(),
                DataType = ws.Cells[row, 3].Text.Trim().ToUpper(),
                ByteOffset = Convert.ToInt32(ws.Cells[row, 4].Value),
                BitOffset = ws.Cells[row, 5].Value is null ? -1 : Convert.ToInt32(ws.Cells[row, 5].Value),
                InitialValue = ws.Cells[row, 6].Text?.Trim() ?? "",
                Comment = ws.Cells[row, 7].Text
            };

            db.Variables.Add(v);
            row++;
        }

        // 先写内存 + 初始值
        foreach (var db in dbDict.Values)
        {
            memory.ApplyDbStructure(db);
            await DatabaseService.Instance.SaveDbAsync(db);
        }

        var totalVars = dbDict.Values.Sum(d => d.Variables.Count);
        var message = $"成功从 Excel 导入 {dbDict.Count} 个 DB,共 {totalVars} 个变量";
        statusCallback?.Invoke(message);
        _log.Info($"Excel import completed: {dbDict.Count} DBs, {totalVars} variables from {excelPath}");
    }
}