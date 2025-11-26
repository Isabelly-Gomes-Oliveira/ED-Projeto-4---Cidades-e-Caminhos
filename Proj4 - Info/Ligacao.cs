using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Proj4
{
  public class Ligacao : IComparable<Ligacao> 
  {
    string origem, destino;
    int distancia;

        public Ligacao(string origem, string destino, int distancia)
        {
            this.origem = origem;
            this.destino = destino;
            this.distancia = distancia;
        }

        public int CompareTo(Ligacao other)
        {
            if (other == null) return 1;
            return (origem+destino).CompareTo(other.origem+other.destino);
        }

        public override string ToString() { return $"Origem: {origem}; Destino: {destino}; Distância: ({distancia} km)"; }
    }
}
