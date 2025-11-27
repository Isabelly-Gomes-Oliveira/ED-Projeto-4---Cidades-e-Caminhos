using AgendaAlfabetica;
using System;
using System.IO;
using System.Windows.Forms;
using System.Text;

namespace Proj4
{
  public class Cidade : IComparable<Cidade>, IRegistro
  {
    string nome;
    double x, y;
    ListaSimples<Ligacao> ligacoes = new ListaSimples<Ligacao>();

    const int tamanhoNome = 25;
    const int tamanhoRegistro = tamanhoNome+ (2 * sizeof(double));

    public string Nome
    {
      get => nome;
      set => nome = value.PadRight(tamanhoNome, ' ').Substring(0, tamanhoNome);
    }

    public Cidade(string nome, double x, double y)
    {
      this.Nome = nome;
      this.x = x;
      this.y = y;
    }
        public ListaSimples<Ligacao> Ligacoes { get => ligacoes; set => ligacoes = value; }
        public override string ToString()
    {
      return Nome.TrimEnd() + " (" + ligacoes.QuantosNos + ")";
    }

    public Cidade()
    {
      this.Nome = "";
      this.x = 0;
      this.y = 0;
    }

    public Cidade(string nome)
    {
      this.Nome = nome;
    }

    public int CompareTo(Cidade outraCid)
    {
      return Nome.CompareTo(outraCid.Nome);
    }

    public int TamanhoRegistro { get => tamanhoRegistro; }
    public double X { get => x; set => x = value; }
    public double Y { get => y; set => y = value; }

    public void LerRegistro(BinaryReader arquivo, long qualRegistro)
    {
            if (arquivo != null)
            {
                try
                {
                    long pos = qualRegistro * TamanhoRegistro;
                    arquivo.BaseStream.Seek(pos, SeekOrigin.Begin);

                    var nomeBytes = arquivo.ReadBytes(25);
                    Nome = Encoding.ASCII.GetString(nomeBytes).TrimEnd(' ', '\0');
                    X = arquivo.ReadSingle();
                    Y = arquivo.ReadSingle();

                    ligacoes = new ListaSimples<Ligacao>(); // inicializa lista de ligações vazia
                }
                catch (Exception e) { 
                    MessageBox.Show("Erro ao ler registro: " + e.Message);
                }
              
            }
         
        }

    public void GravarRegistro(BinaryWriter arquivo)
    {
            if (arquivo != null)
            {
                var nomeBytes = new byte[25];
                var b = Encoding.ASCII.GetBytes((Nome ?? "").PadRight(25).Substring(0, 25));

                Array.Copy(b, nomeBytes, Math.Min(b.Length, nomeBytes.Length));

                arquivo.Write(nomeBytes);
                arquivo.Write(X);
                arquivo.Write(Y);
            }
  
    }

    public void InserirLigacao(string origem, string destino, int distancia)
    {
        Ligacao novaLigacao = new Ligacao(origem, destino, distancia);
        ligacoes.InserirEmOrdem(novaLigacao);
    }

     public void RemoverLigacao(string origem, string destino)
     {
         ligacoes.RemoverDado(new Ligacao(origem, destino));
     }

    }

}
