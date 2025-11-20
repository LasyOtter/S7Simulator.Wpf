using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace S7Simulator.Wpf.Models;

public class DbInfo
{
    public int DbNumber { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime ImportedAt { get; set; }
    public List<VariableInfo> Variables { get; set; } = new();
}
