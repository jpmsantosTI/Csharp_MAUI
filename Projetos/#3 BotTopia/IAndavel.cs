using System;
using System.Linq;
using System.Collections.Generic;

namespace BotTopia;

public interface IAndavel
{
    public int X { get; set; }
    public int Y { get; set; }
    public string Simbolo { get; set; }
    void Mover() { }
}
