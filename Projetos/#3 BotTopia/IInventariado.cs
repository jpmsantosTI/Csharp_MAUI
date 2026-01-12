using System;
using System.Linq;
using System.Collections.Generic;

namespace BotTopia;

public interface IInventariado
{
    List<Item> Inventario { get; set; }
    void PegarItem(Item i) { }
    void RemoverItem(Item i) { }
}

