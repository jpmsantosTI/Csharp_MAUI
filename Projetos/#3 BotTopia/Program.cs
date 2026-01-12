using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using BotTopia;
using System.Data.Common;
using System.Diagnostics;

namespace BotTopia
{

    public static class Pathfinding
    {
        // Retorna lista de posições do caminho ou null se não existe
        public static List<(int x, int y)> BFS(int startX, int startY, int targetX, int targetY)
        {
            int largura = Mapa.grid.GetLength(1); // X
            int altura = Mapa.grid.GetLength(0);  // Y

            var fila = new Queue<(int x, int y)>();
            var veioDe = new Dictionary<(int, int), (int, int)>();
            var visitado = new bool[altura, largura];

            fila.Enqueue((startX, startY));
            visitado[startY, startX] = true;

            var dirs = new (int x, int y)[]
            {  (0,1),(0,-1),(1,0),(-1,0)  };

            while (fila.Count > 0)
            {
                var atual = fila.Dequeue();

                if (atual.x == targetX && atual.y == targetY)
                    return ReconstruirCaminho(veioDe, (startX, startY), (targetX, targetY));

                foreach (var d in dirs)
                {
                    int nx = atual.x + d.x;
                    int ny = atual.y + d.y;

                    if (Mapa.PosicaoValida(nx, ny) && !visitado[ny, nx])
                    {
                        visitado[ny, nx] = true;
                        fila.Enqueue((nx, ny));
                        veioDe[(nx, ny)] = atual;
                    }
                }
            }

            return null;
        }

        private static List<(int x, int y)> ReconstruirCaminho(Dictionary<(int, int), (int, int)> veioDe, (int, int) start, (int, int) end)
        {
            var caminho = new List<(int x, int y)>();
            var atual = end;

            while (!atual.Equals(start))
            {
                caminho.Add(atual);
                atual = veioDe[atual];
            }

            caminho.Add(start);
            caminho.Reverse();
            return caminho;
        }
    }

    public static class Helper
    {
        public static void RemoverValorDaFila<T>(Queue<T> fila, T valor)
        {
            Queue<T> novaFila = new Queue<T>();

            foreach (var item in fila)
            {
                if (!item.Equals(valor))
                    novaFila.Enqueue(item);
            }

            fila.Clear();

            foreach (var item in novaFila)
                fila.Enqueue(item);
        }

        public static void PrintListaOrdenada(List<Item> lista, int comeco = 1)
        {
            for (int i = 0; i < lista.Count(); i++)
            {
                Item it = lista[i];
                Console.WriteLine($"{i + comeco} – {it.Nome}");
            }
        }

        public static void PrintFilaOrdenada<T>(Queue<T> fila, int comeco = 1)
        {
            Queue<T> f = new Queue<T>(fila);
            for (int i = 0; i < f.Count(); i++)
            {
                Console.WriteLine($"{i + comeco} – {f.Dequeue()}");
            }
        }
    }

    public static class Mapa
    {
        public static int Tamanho = 7;
        public static Tile[,] grid = new Tile[Tamanho, Tamanho];
        public static List<Bot> Bots = new List<Bot>();
        public static List<Player> Players = new();
        public static List<Tile> Tiles = new();


        public static void Inicializar()
        {
            for (int y = 0; y < Tamanho; y++)
            {
                for (int x = 0; x < Tamanho; x++)
                {
                    grid[y, x] = new Tile();
                }
            }
            grid[3, 0] = new ItemLargado(new Bateria());
            grid[4, 1] = new ItemLargado(new Bateria());
            grid[2, 3] = new Deposito(1, recursos: new Item[] { new Bateria() });
            grid[0, 0] = new Deposito(7, recursos: new Item[] { new Bateria() });
            grid[4, 4] = new Deposito(10, recursos: new Item[] { new Bateria() });
            grid[2, 2] = new GerenciadorDeBots();
        }

        public static void Mostrar()
        {
            for (int y = 0; y < Tamanho; y++)
            {
                for (int x = 0; x < Tamanho; x++)
                {
                    if (Players.Any(p => p.X == x && p.Y == y))
                    {
                        Console.Write(Players.FirstOrDefault(p => p.X == x && p.Y == y).Simbolo);
                    }
                    else if (Bots.Any(b => b.X == x && b.Y == y))
                    {
                        Console.Write(Bots.FirstOrDefault(b => b.X == x && b.Y == y).Simbolo);
                    }
                    else { Console.Write(grid[y, x].Simbolo); }
                }
                Console.WriteLine();
            }
        }
        public static bool PosicaoValida(int x, int y)
        {
            return x < Tamanho && x >= 0 && y < Tamanho && y >= 0;
        }
        public static int MedirDistancia(int PosX1, int PosY1, int PosX2, int PosY2)
        {
            return Math.Abs(PosX1 - PosX2) + Math.Abs(PosY1 - PosY2);
        }
    }

    public class Tile
    {
        public string Simbolo;
        public bool Bloqueia;
        public List<Item> Recursos = new();
        public IEntidadeCompleta ReservadoPor;
        public Tile(string simbolo = "⬜", bool bloqueia = false, params Item[] recursos)
        {
            Simbolo = simbolo;
            Bloqueia = bloqueia;
            Recursos.AddRange(recursos);
        }
        public virtual void Interagir(IEntidadeCompleta e) { }

        public void EscolherRecursos(Player p)
        {
            Console.Clear();
            Console.WriteLine("Selecione o item:\n");

            Console.WriteLine("0 – Sair");
            for (int i = 0; i < Recursos.Count(); i++)
            {
                Console.WriteLine($"{i + 1} – {Recursos[i].Nome}");
            }

            int indice;
            int qtd;
            try
            {
                try { indice = int.Parse(Console.ReadLine()); } catch { indice = 0; }
                if (indice != 0)
                {
                    var it = Recursos[indice - 1].Clone();


                    if (it.Empilhavel)
                    {
                        Console.Clear();
                        Console.WriteLine($"{it.Nome}(x{it.Quantidade})\nquantidade a ser pega:\n");
                        try { qtd = int.Parse(Console.ReadLine()); }
                        catch { qtd = 0; }

                        if (qtd > it.Quantidade || qtd < 0) qtd = 1;

                        if (qtd > 0)
                        {
                            it.Quantidade = qtd;
                            p.PegarItem(it);
                            Recursos[indice - 1].Quantidade -= qtd;
                        }

                        if (Recursos[indice - 1].Quantidade <= 0) Recursos.Remove(Recursos[indice - 1]);

                    }
                    else
                    {
                        p.Inventario.Add(it);
                        RemoverItem(Recursos[indice - 1]);
                    }
                }
            }
            catch { Console.WriteLine("Item não existe"); Thread.Sleep(2000); }

        }

        public void AdicionarItem(Item item)
        {
            if (item.Quantidade >= 0)
            {

                if (item.Empilhavel)
                {
                    var itemEncontrado = Recursos.FirstOrDefault(i => i.Nome.ToLower() == item.Nome.ToLower());
                    if (itemEncontrado != null)
                    {
                        itemEncontrado.Adicionar(item.Quantidade);
                    }
                    else
                    {
                        Recursos.Add(item.Clone());
                    }
                }
                else
                {
                    Recursos.Add(item.Clone());
                }
            }
        }
        public void RemoverItem(Item item)
        {
            item.Quantidade--;
            if (item.Quantidade <= 0)
                Recursos.Remove(item);
        }
    }

    public class Deposito : Tile
    {
        private int TickPraCriar;
        private int Tick = 0;
        List<Item> RecursosOriginais = new();

        public Deposito(int tickPraCriar, string simbolo = "🔲", params Item[] recursos) : base(simbolo)
        {
            TickPraCriar = tickPraCriar;
            Program.Tick += Fabricar;
            foreach (Item i in recursos)
            {
                RecursosOriginais.Add(i);
                Recursos.Add(i.Clone());
            }
        }

        public void Fabricar()
        {
            if (Tick == TickPraCriar)
            {
                foreach (Item i in RecursosOriginais)
                {
                    AdicionarItem(i.Clone());
                }
                Tick = 0;
            }
            else
            {
                Tick++;
            }
        }

        public override void Interagir(IEntidadeCompleta e)
        {
            Item i;

            if (e is Bot b)
            {
                {
                    i = Recursos.FirstOrDefault(r => r.GetType() == b.ItemDesejado);
                    var clone = i.Clone();
                    clone.Quantidade = 1;
                    b.PegarItem(clone);
                    RemoverItem(i);
                }
            }
            if (e is Player p)
            {
                EscolherRecursos(p);
            }

        }
    }
    public class GerenciadorDeBots : Tile
    {
        public static List<Bot> Bots = Mapa.Bots;
        public GerenciadorDeBots() : base("🛅") { }
        public override void Interagir(IEntidadeCompleta e)
        {
            Console.Clear();
            if (e is Player p)
            {
                Console.WriteLine("Escolha o robô a ser comandado:");
                for (int i = 0; i < Bots.Count(); i++)
                {
                    Bot b = Bots[i];
                    Console.WriteLine($"{b.Id} – Bot {b.Id} {b.Nome} (X{b.X}, Y{b.Y}) Objetivo: {b.ObjetivoAtual}");
                }
                int escolha = int.Parse(Console.ReadLine());
                try
                {
                    Bot b = Bots.FirstOrDefault(r => r.Id == escolha);
                    b.Debug();
                    Console.WriteLine("Escolha umas das opções:");
                    Console.WriteLine("1 – Adicionar objetivo\n2 – Ver fila de objetivos\n3 – Desligar\n");
                    escolha = int.Parse(Console.ReadLine());

                    if (escolha == 1)//adicionar objetivo
                    {
                        Console.WriteLine("\n1 – Procurar Item\n2 – Devolver itens\n");
                        escolha = int.Parse(Console.ReadLine());
                        if (escolha == 1) // procurar item
                        {
                            Console.WriteLine("Escolha o tipo de item desejado");
                            Helper.PrintListaOrdenada(p.ItensConhecidos);
                            try
                            {
                                escolha = int.Parse(Console.ReadLine());
                                Item ItemDesejado = p.ItensConhecidos[escolha - 1];
                                Console.WriteLine("Quantidade desejada:");
                                escolha = int.Parse(Console.ReadLine());
                                for (int i = 0; i < escolha; i++)
                                {
                                    b.AdicionarProcuraPorItem(ItemDesejado);
                                }
                            }
                            catch { }
                        }
                        else if (escolha == 2) // devolver itens pra algum lugar
                        {
                            b.Objetivo.Enqueue(Bot.Objetivos.VoltarProPlayer);
                        }
                    }

                    else if (escolha == 2) //ver lista de objetivos
                    {
                        Console.WriteLine("\nObjetivos:");
                        Helper.PrintFilaOrdenada<Bot.Objetivos>(b.Objetivo);
                        Thread.Sleep(2000);
                    }
                }
                catch { }
            }
        }
    }
    public class ItemLargado : Tile
    {
        public Item Item;

        public ItemLargado(Item item) : base(item.Simbolo)
        {
            Item = item.Clone();
        }
        public override void Interagir(IEntidadeCompleta i)
        {
            i.PegarItem(Item.Clone());
            Mapa.grid[i.Y, i.X] = new Tile();
            Mapa.grid[i.Y, i.X].ReservadoPor = null;
        }
    }




    public class Item
    {
        public string Nome { get; set; }
        public string Simbolo;
        public string Descricao { get; protected set; }
        public bool Empilhavel { get; protected set; }
        public int Quantidade { get; set; }
        public bool MataFome { get; protected set; }
        public bool MataSede { get; protected set; }

        public Item(string nome, bool empilhavel, string simbolo, string descricao = "", int quantidade = 1, bool mataSede = false, bool mataFome = false)
        {
            Nome = nome;
            Simbolo = simbolo;
            Descricao = descricao;
            Quantidade = quantidade;
            Empilhavel = empilhavel;
            MataFome = mataFome;
            MataSede = mataSede;
        }
        public virtual void Usar(TemInventario i)
        {
            if (Empilhavel)
            {
                Quantidade--;
                if (Quantidade <= 0)
                { i.RemoverItem(this); }
            }
        }
        public void Adicionar(int quantidade)
        {
            Quantidade += quantidade;
        }
        public virtual Item Clone()
        {
            return new Item(
                Nome,
                Empilhavel,
                Simbolo,
                Descricao,
                Quantidade,
                MataSede,
                MataFome
            );
        }
    }
    public class Bateria : Item
    {
        public int Amperes { get; private set; }
        public Bateria(int amperes = 25) : base($"Bateria de {amperes} amperes", true, "🔋")
        {
            Amperes = amperes;
        }
        public override void Usar(TemInventario i)
        {
            base.Usar(i);
            if (i is Bot b)
            {
                b.Energia += Amperes;
            }
        }
        public override Item Clone()
        {
            Bateria clone = new Bateria(Amperes);
            clone.Quantidade = Quantidade;
            return clone;
        }
    }




    public abstract class TemInventario : IInventariado
    {
        public List<Item> Inventario { get; set; } = new();
        public List<Item> ItensConhecidos { get; set; } = new();

        public void PegarItem(Item item)
        {
            if (item.Quantidade >= 0)
            {
                if (item.Empilhavel)
                {
                    var itemEncontrado = Inventario.FirstOrDefault(i => i.Nome.ToLower() == item.Nome.ToLower());
                    if (itemEncontrado != null)
                    {
                        itemEncontrado.Adicionar(item.Quantidade);
                    }
                    else
                    {
                        Inventario.Add(item);
                    }
                }
                else
                {
                    Inventario.Add(item);
                }
                if (!ItensConhecidos.Any(i => i.GetType() == item.GetType()))
                {
                    Item ic = item.Clone();
                    ic.Nome = $"{ic.GetType().Name}";
                    ItensConhecidos.Add(ic);
                }
            }
        }
        public void RemoverItem(Item item)
        {
            item.Quantidade--;
            if (item.Quantidade <= 0)
                Inventario.Remove(item);
        }
        public void MostrarInventario()
        {
            for (int i = 0; i < Inventario.Count(); i++)
            {
                var item = Inventario[i];
                Console.Write($"{i + 1} – {item.Nome}");
                if (item.Empilhavel)
                {
                    Console.Write($"(x{item.Quantidade})\n");
                }
                else { Console.WriteLine(item); }
            }
            Console.WriteLine($"conhecidos: {String.Join(",", ItensConhecidos)}");
        }
    }




    public class Bot : TemInventario, IEntidadeCompleta
    {
        public static List<string> NomesPossiveis = new List<string>() { "Vitor", "Augusto", "Beto", "Vetor" }; //nomes aleatorios pros bots
        public string Nome = NomesPossiveis[new Random().Next(NomesPossiveis.Count())];

        public int Id;
        public int X { get; set; }
        public int Y { get; set; }
        public string Simbolo { get; set; } = "🤖";

        private int Tick = 0;

        public Type? ItemDesejado;
        public int Energia;

        public List<Bot> BotsProximos = new();
        public List<Bot> BotsNecessitados = new();
        public List<Bot> BotsVindoAjudar = new();
        private static List<Bot> BotsQueExistiram = new();

        public Queue<Objetivos> BackupDeObjetivos = new Queue<Objetivos>();
        public Queue<Objetivos> Objetivo = new Queue<Objetivos>();
        public Objetivos? ObjetivoAtual;

        public string FaseDoObjetivo;

        public Action<Bot> FoiAjudado;

        public enum Objetivos
        {
            PrecisaDeAjuda,
            VouAjudar,
            FabricarBateria,
            PegarBateria,
            FabricarItem,
            VoltarProPlayer,
            Desligado,
            Nenhum
        }

        public Bot(int x, int y, int energia = 100, int id = 999)
        {
            X = x;
            Y = y;
            Energia = energia;
            if (id == 999)
            {
                id = 0;
                while (BotsQueExistiram.Any(b => b.Id == id) || id == 0)
                {
                    id++;
                }
            }
            Id = id;
            BotsQueExistiram.Add(this);

        }
        public void Debug()
        {
            Console.WriteLine($"Bot {Id}:");
            Console.WriteLine($"Nome: {Nome}");
            Console.WriteLine($"ObjetivoAtual: {ObjetivoAtual}");
            Console.WriteLine($"Fase do objetivo: {FaseDoObjetivo}");
            if (ItemDesejado != null) Console.WriteLine($"Item desejado: {ItemDesejado.Name}");
            else { Console.WriteLine("Não quer item nenhum"); }
            Console.WriteLine($"Energia: {Energia}");
            Console.WriteLine("Bots próximos de : " + string.Join(",", BotsProximos.Select(b => b.Id)));
            Console.WriteLine("Bots necessitados : " + string.Join(",", BotsNecessitados.Select(b => b.Id)));
            Console.WriteLine("Bots Vindo ajudar : " + string.Join(",", BotsVindoAjudar.Select(b => b.Id)));
            Console.WriteLine("itens: " + string.Join(",", Inventario.Select(b => b.Quantidade)));
            Console.WriteLine("=========");
        }
        public void Status()
        {
            Console.WriteLine($"Bot {Id}:");
            Console.WriteLine($"Nome: {Nome}");
            Console.WriteLine($"ObjetivoAtual: {ObjetivoAtual}");
            Console.WriteLine($"Fase do objetivo: {FaseDoObjetivo}");
            if (ItemDesejado != null) Console.WriteLine($"Item desejado: {ItemDesejado.Name}");
            else { Console.WriteLine("Não deseja item nenhum"); }
            Console.WriteLine($"Energia: {Energia}");
            Console.WriteLine("Inventario:");
            foreach (Item i in Inventario)
            {
                Console.Write($"{i}({i.Quantidade}x) ");
            }
            Console.WriteLine("=========");
        }
        public void Iniciar()
        {
            Mapa.Bots.Add(this);
            Program.Tick += Viver;
            //AdicionarObjetivoInRange(2, Objetivos.PegarBateria);
            //Objetivo.Enqueue(Objetivos.VoltarProPlayer);
            Program.Tick += Debug;
        }
        public void Encerrar()
        {
            Mapa.Bots.Remove(this);
            Program.Tick -= Viver;
            Objetivo.Clear();
            ObjetivoAtual = Objetivos.Desligado;
        }

        public void Viver()
        {
            Tick++;

            VerificarBotsProximos();
            if (Objetivo.Count() > 0) { ObjetivoAtual = Objetivo.Peek(); }
            else { ObjetivoAtual = Objetivos.Nenhum; }

            if (Energia > 0 && ObjetivoAtual != Objetivos.PrecisaDeAjuda)
            {
                foreach (Objetivos o in BackupDeObjetivos)
                {
                    Objetivo.Enqueue(o);
                }
                BackupDeObjetivos.Clear();
                if (Objetivo.Count() > 0) Energia--;
                else if (Tick % 2 == 0) { Energia--; }

                ExecutarObjetivo();
            }
            if (Energia < 20)
            {
                Item i = Inventario.FirstOrDefault(i => i is Bateria);
                if (i != null) i.Usar(this);
                if (ObjetivoAtual == Objetivos.Nenhum)
                {
                    Objetivo.Enqueue(Objetivos.PegarBateria);
                }
            }
            if (Energia <= 0 || (Energia <= 20 && ObjetivoAtual == Objetivos.PrecisaDeAjuda))
            {
                if (BackupDeObjetivos.Count() == 0)
                {
                    foreach (Objetivos o in Objetivo)
                    {
                        BackupDeObjetivos.Enqueue(o);
                    }
                    Objetivo.Clear();
                    TirarReserva();
                }
                if (ObjetivoAtual != Objetivos.PrecisaDeAjuda)
                    Objetivo.Enqueue(Objetivos.PrecisaDeAjuda);
                FaseDoObjetivo = "Pixels de tristeza";
                ItemDesejado = typeof(Bateria);
                ExecutarObjetivo();
            }
            if (Energia >= 20 && ObjetivoAtual == Objetivos.PrecisaDeAjuda)
            {
                FuiAjudado();
            }

        }

        public void Mover((int x, int y)? alvo)
        {
            if (alvo == null) { return; }
            var caminho = Pathfinding.BFS(X, Y, alvo.Value.x, alvo.Value.y);
            if (caminho != null && caminho.Count > 1)
            {
                var proximoPasso = caminho[1];
                X = proximoPasso.x;
                Y = proximoPasso.y;
            }
        }
        public void AdicionarProcuraPorTipo<T>() where T : Item
        {
            Type itemDesejado = typeof(T);
            if (itemDesejado is Bateria) Objetivo.Enqueue(Objetivos.PegarBateria);
        }
        public void AdicionarProcuraPorItem(Item item)
        {
            if (item is Bateria) Objetivo.Enqueue(Objetivos.PegarBateria);
        }
        public (int x, int y)? ProcurarItem<T>() where T : Item
        {
            List<(int x, int y)> possiveis = new();
            ItemDesejado = typeof(T);
            FaseDoObjetivo = $"Procurando {typeof(T).Name}";
            for (int y = 0; y < Mapa.Tamanho; y++)
            {
                for (int x = 0; x < Mapa.Tamanho; x++)
                {
                    if (Mapa.grid[y, x].Recursos.Any(r => r is T) && (Mapa.grid[y, x].ReservadoPor == null || Mapa.grid[y, x].ReservadoPor == this)) { possiveis.Add((x, y)); }
                    if (Mapa.grid[y, x] is ItemLargado il && il.Item is T && (Mapa.grid[y, x].ReservadoPor == null || Mapa.grid[y, x].ReservadoPor == this)) { possiveis.Add((x, y)); }
                }
            }

            if (possiveis.Count() == 0) { FaseDoObjetivo = $"{typeof(T).Name} não encontrado(a)"; return null; }
            FaseDoObjetivo = $"se movendo até o item {typeof(T).Name}";
            (int x, int y) local = possiveis.OrderBy(p => Math.Abs(X - p.x) + Math.Abs(Y - p.y)).First();
            Mapa.grid[local.y, local.x].ReservadoPor = this;

            if (local == (X, Y))
            {
                if (Mapa.grid[Y, X] is ItemLargado il) { il.Interagir(this); }
                else if (Mapa.grid[Y, X] is Deposito d) { d.Interagir(this); }
                // return ProcurarItem<T>();   isso faz pegar o item direto quando passa
                Mapa.grid[local.y, local.x].ReservadoPor = null;
                FaseDoObjetivo = $"Pegando {ItemDesejado.Name}";

                if (ObjetivoAtual != Objetivos.VouAjudar && local == (X, Y))
                    Objetivo.Dequeue();
            }

            return local;
        }

        public void AdicionarObjetivos(params Objetivos[] objetivos)
        {
            foreach (Objetivos o in objetivos)
            {
                Objetivo.Enqueue(o);
            }
        }
        public void AdicionarObjetivoInRange(int n, Objetivos o)
        {
            for (int i = 0; i < n; i++)
            {
                Objetivo.Enqueue(o);
            }
        }

        public void ExecutarObjetivo()
        {
            ItemDesejado = null;
            if (Objetivo.Count() > 0)
            {
                if (Objetivo.Peek() == Objetivos.PegarBateria)
                {
                    Mover(ProcurarItem<Bateria>());
                }

                else if (Objetivo.Peek() == Objetivos.VoltarProPlayer)
                {
                    Mover((Mapa.Players[0].X, Mapa.Players[0].Y));
                    if ((X, Y) == (Mapa.Players[0].X, Mapa.Players[0].Y))
                    {
                        foreach (Item i in Inventario)
                        {
                            Mapa.Players[0].PegarItem(i.Clone());
                        }
                        Inventario.Clear();
                        Objetivo.Dequeue();
                    }
                }
                else if (Objetivo.Peek() == Objetivos.PrecisaDeAjuda)
                {
                    if (BotsVindoAjudar.Count() == 0 || BotsVindoAjudar.All(b => b.ObjetivoAtual == Objetivos.PrecisaDeAjuda || b.ObjetivoAtual == Objetivos.Desligado))
                    {
                        foreach (Bot b in BotsProximos.Where(b => (b.ObjetivoAtual != Objetivos.PrecisaDeAjuda && b.ObjetivoAtual != Objetivos.Desligado)).ToList())
                        {
                            b.Objetivo.Enqueue(Objetivos.VouAjudar);
                            if (!b.BotsNecessitados.Contains(this))
                                b.BotsNecessitados.Add(this);
                            FoiAjudado += b.DeixarDeAjudar;
                        }
                    }
                }
                else if (Objetivo.Peek() == Objetivos.VouAjudar)
                {
                    if (BotsNecessitados.Count() > 0)
                    {
                        Bot b = BotsNecessitados[0];
                        if (!b.BotsVindoAjudar.Contains(this))
                        {
                            b.BotsVindoAjudar.Add(this);
                        }
                        if (Inventario.Any(i => i is Bateria))
                        {
                            Item bateria = Inventario.FirstOrDefault(i => i is Bateria);
                            Mover((b.X, b.Y));
                            FaseDoObjetivo = "indo até o bot";
                            if ((X, Y) == (b.X, b.Y))
                            {
                                FaseDoObjetivo = "Entregando bateria";
                                EntregarProBot(b, bateria);
                            }
                        }
                        else
                        {
                            ItemDesejado = typeof(Bateria);
                            Mover(ProcurarItem<Bateria>());
                        }
                    }
                    else { Objetivo.Dequeue(); }
                }
            }
            else
            {
                FaseDoObjetivo = "nenhuma";
            }
        }

        public void DeixarDeAjudar(Bot b)
        {
            b.FoiAjudado -= DeixarDeAjudar;
            BotsNecessitados.Remove(b);
        }
        public void FuiAjudado()
        {
            Objetivo.Dequeue();
            BotsVindoAjudar.Clear();
            FoiAjudado?.Invoke(this);
        }
        public void VerificarBotsProximos()
        {
            List<Bot> botsProximos = Mapa.Bots.Where(b => b != this).OrderBy(b => { return Mapa.MedirDistancia(X, b.X, Y, b.Y); }).ToList();

            BotsProximos = botsProximos.OrderBy(b => b.Id).ToList();
        }
        public void EntregarProBot(Bot b, Item i, int qtd = 1)
        {
            if (qtd > i.Quantidade) qtd = i.Quantidade;

            Item Entrega = i.Clone();
            Entrega.Quantidade = qtd;

            b.PegarItem(Entrega);

            for (int j = 0; j < qtd; j++)
            {
                RemoverItem(i);
            }
        }
        public void TirarReserva()
        {
            for (int y = 0; y < Mapa.Tamanho; y++)
            {
                for (int x = 0; x < Mapa.Tamanho; x++)
                {
                    if (Mapa.grid[y, x].ReservadoPor == this)
                    {
                        Mapa.grid[y, x].ReservadoPor = null;
                    }
                }
            }
        }
    }


    public class Player : TemInventario, IEntidadeCompleta
    {
        public string Nome;
        public string Simbolo { get; set; } = "👤";
        public int X { get; set; }
        public int Y { get; set; }

        public Player(string nome, int x, int y)
        {
            Nome = nome;
            X = x;
            Y = y;
            Mapa.Players.Add(this);
        }
        public void Mover(string direcao)
        {
            int novoX = X;
            int novoY = Y;
            switch (direcao.ToLower())
            {
                case "w": // cima
                    novoY--;
                    break;
                case "s": // baixo
                    novoY++;
                    break;
                case "a": // esquerda
                    novoX--;
                    break;
                case "d": // direita
                    novoX++;
                    break;
            }
            if (Mapa.PosicaoValida(novoX, novoY))
            {
                X = novoX;
                Y = novoY;
            }
            else
            {
                Console.WriteLine("Irmão, não dá pra passar pra fora do mapa");
                Thread.Sleep(2000);
            }
        }
        public void Interagir()
        {
            Mapa.grid[Y, X].Interagir(this);
        }

        public void MexerNoInventario()
        {
            Console.Clear();
            Console.WriteLine("Escolha o item:");
            Console.WriteLine("0 – sair");
            MostrarInventario();
            int indice;

            try
            {
                indice = int.Parse(Console.ReadLine());

                if (indice != 0)
                {
                    MostrarItem(indice - 1);

                    Console.WriteLine("1 - usar\n2 - remover\n3 - largar");
                    string escolha = Console.ReadLine();
                    if (escolha == "1")
                    {
                        Inventario[indice - 1].Usar(this);
                    }
                    else if (escolha == "2")
                    {
                        Console.WriteLine("Escolha a quantidade a ser excluida:");
                        int qtd = int.Parse(Console.ReadLine());

                        for (int i = 0; i < qtd; i++)
                        {
                            RemoverItem(Inventario[indice - 1]);
                        }
                    }
                    else if (escolha == "3")
                    {
                        var it = Inventario[indice - 1].Clone();

                        Console.WriteLine("Escolha a quantidade a ser largada:");
                        int qtd = int.Parse(Console.ReadLine());

                        if (qtd > Inventario[indice - 1].Quantidade)
                        {
                            qtd = Inventario[indice - 1].Quantidade;
                        }
                        it.Quantidade = qtd;

                        if (Mapa.grid[Y, X] is Deposito d) { d.AdicionarItem(it); }
                        else { Mapa.grid[Y, X] = new ItemLargado(it); }

                        for (int i = 0; i < qtd; i++)
                        { RemoverItem(Inventario[indice - 1]); }
                    }
                }
            }
            catch { indice = 0; }
        }

        public void MostrarItem(int indice)
        {
            Item i = Inventario[indice];
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write($"===== {i.Nome} =====\n{i.Descricao}\n=====");
            for (int l = 0; l < i.Nome.Length; l++)
            {
                Console.Write("=");
            }
            Console.Write("=======\n");
            Console.ResetColor();
        }

    }

    public class Program
    {
        public static Action Tick;
        public static void Main()
        {
            new Bot(4, 0).Iniciar();
            new Bot(4, 2, 90).Iniciar();
            new Bot(0, 4, 0).Iniciar();

            //player cria
            string nome;
            Console.WriteLine("Seu NickName:");
            nome = Console.ReadLine();
            Player P1 = new Player(nome, 2, 2);

            //Comeco do jogo
            Mapa.Inicializar();
            bool Debugando = true;
            string escolha;
            int repeticoes = 1;
            while (true)
            {
                for (int i = 0; i < repeticoes; i++)
                {
                    Console.Clear();
                    Tick?.Invoke();
                    Mapa.Mostrar();
                    if (repeticoes == 1) Thread.Sleep(500);
                }
                repeticoes = 1;
                Console.WriteLine("wasd – Mover");
                Console.WriteLine("loop – Rodar loop");
                Console.WriteLine("e – Interagir");
                Console.WriteLine("i – inventario");
                Console.WriteLine("db – debug");
                Console.WriteLine();
                escolha = Console.ReadLine().ToLower();

                if (escolha == "w" || escolha == "a" || escolha == "s" || escolha == "d")
                {
                    P1.Mover(escolha);
                }
                else if (escolha == "loop")
                {
                    Console.WriteLine("quantidade de loops:");
                    try
                    {
                        repeticoes = int.Parse(Console.ReadLine());
                        if (repeticoes == 0) { repeticoes = 1; }
                    }
                    catch { repeticoes = 1; }
                }
                else if (escolha == "e")
                {
                    P1.Interagir();
                }
                else if (escolha == "i")
                {
                    P1.MexerNoInventario();
                }
                else if (escolha == "db")
                {
                    if (Debugando)
                    {
                        foreach (Bot b in Mapa.Bots)
                        { Tick -= b.Debug; }
                        Debugando = false;
                    }
                    else
                    {
                        foreach (Bot b in Mapa.Bots)
                        { Tick += b.Debug; }
                        Debugando = true;
                    }
                }
            }
        }
    }
}