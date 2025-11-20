using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace S7Simulator.Wpf.Models;

public class VariableInfo
{
    public int Id { get; set; }
    public int DbNumber { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public int ByteOffset { get; set; }
    public int BitOffset { get; set; } = -1;
    public string InitialValue { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;
}
