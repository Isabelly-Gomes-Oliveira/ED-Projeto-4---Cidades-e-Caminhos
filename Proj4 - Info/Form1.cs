using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using AgendaAlfabetica;

namespace Proj4
{
    public partial class Form1 : Form
    {
        Arvore<Cidade> minhaArvore = new Arvore<Cidade>();

        enum EstadoApp { Navegando, AguardandoNome, AguardandoPontoMapa }
        EstadoApp estadoAtual = EstadoApp.Navegando;

        Cidade cidSelecionada = null;

        List<Cidade> cacheCidades = new List<Cidade>();
        int[,] matrizAdjacencia;
        const int INF = int.MaxValue;

        // Variáveis auxiliares para o Dijkstra
        int[] distanciasMinimas;
        string[] cidadesPai;
        bool[] visitados;

        public Form1()
        {
            InitializeComponent();
        }

        private void tpCadastro_Click(object sender, EventArgs e) { }
        private void label2_Click(object sender, EventArgs e) { }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (dlgAbrir.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    minhaArvore.LerArquivoDeRegistros(dlgAbrir.FileName);

                    RecarregarCache();

                    if (dlgAbrir.ShowDialog() == DialogResult.OK)
                        CarregarArestas(dlgAbrir.FileName);

                    SincronizarGrafo();
                    CarregarComboDestinos();
                }
                catch
                {
                }
            }

            if (matrizAdjacencia == null) InicializarGrafoVazio();
        }

        private void RecarregarCache()
        {
            cacheCidades.Clear();
            minhaArvore.VisitarEmOrdem(cacheCidades);
            InicializarGrafoVazio();
        }

        private void InicializarGrafoVazio()
        {
            int n = cacheCidades.Count;
            matrizAdjacencia = new int[n, n];

            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                    matrizAdjacencia[i, j] = INF;
        }

        private void CarregarArestas(string path)
        {
            var leitor = new StreamReader(path);
            while (!leitor.EndOfStream)
            {
                string linha = leitor.ReadLine();

                var dados = linha.Split(';');
                if (dados.Length >= 3)
                {
                    string de = dados[0].Trim();
                    string para = dados[1].Trim();
                    int km = int.Parse(dados[2]);

                    if (minhaArvore.Existe(new Cidade(de)))
                    {
                        minhaArvore.Atual.Info.InserirLigacao(de, para, km);
                        if (minhaArvore.Existe(new Cidade(para)))
                            minhaArvore.Atual.Info.InserirLigacao(para, de, km);
                    }
                }
            }
            leitor.Close();
        }

        private void SincronizarGrafo()
        {
            int n = cacheCidades.Count;
            for (int i = 0; i < n; i++)
            {
                var lista = cacheCidades[i].Ligacoes;
                if (lista != null && !lista.EstaVazia)
                {
                    var cursor = lista.Primeiro;
                    while (cursor != null)
                    {
                        var lig = cursor.Info;
                        int idxDestino = cacheCidades.FindIndex(c => c.Nome.Trim() == lig.Destino.Trim());

                        if (idxDestino >= 0)
                            matrizAdjacencia[i, idxDestino] = lig.Distancia;

                        cursor = cursor.Prox;
                    }
                }
            }
        }

        private void pnlArvore_Paint(object sender, PaintEventArgs e)
        {
            minhaArvore.Desenhar(pnlArvore);
        }

        private void btnIncluirCidade_Click(object sender, EventArgs e)
        {
            if (txtNomeCidade.Text.Length > 0)
            {
                if (!minhaArvore.Existe(new Cidade(txtNomeCidade.Text)))
                {
                    if (MessageBox.Show("Manter nome?", "Confirmação", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        if (udX.Value == 0 && udY.Value == 0)
                        {
                            estadoAtual = EstadoApp.AguardandoPontoMapa;
                            MessageBox.Show("Selecione a posição no mapa.");
                        }
                        else
                        {
                            SalvarNovaCidade(new Cidade(txtNomeCidade.Text, (double)udX.Value, (double)udY.Value));
                        }
                    }
                    else
                    {
                        estadoAtual = EstadoApp.AguardandoNome;
                        txtNomeCidade.Focus();
                    }
                }
                else
                    MessageBox.Show("Cidade duplicada!");
            }
            else
            {
                estadoAtual = EstadoApp.AguardandoNome;
                txtNomeCidade.Focus();
            }
        }

        private void SalvarNovaCidade(Cidade c)
        {
            minhaArvore.IncluirNovoDado(c);
            RecarregarCache();
            SincronizarGrafo();

            txtNomeCidade.Text = "";
            udX.Value = 0; udY.Value = 0;
            estadoAtual = EstadoApp.Navegando;
            CarregarComboDestinos();

            MessageBox.Show("Salvo.");
            pbMapa.Invalidate();
            pnlArvore.Invalidate();
        }

        private void pbMapa_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Font fonte = new Font("Arial", 9);

            foreach (var c in cacheCidades)
            {
                float px = (float)(pbMapa.Width * c.X);
                float py = (float)(pbMapa.Height * c.Y);

                g.FillEllipse(Brushes.Black, px - 3, py - 3, 6, 6);
                g.DrawString(c.Nome.Trim(), fonte, Brushes.Black, px + 4, py - 4);

                DesenharArestas(g, c, px, py);
            }

            if (estadoAtual == EstadoApp.AguardandoPontoMapa || estadoAtual == EstadoApp.Navegando)
            {
                float sx = (float)((double)udX.Value * pbMapa.Width);
                float sy = (float)((double)udY.Value * pbMapa.Height);
                g.FillEllipse(Brushes.Green, sx - 3, sy - 3, 6, 6);
            }

            if (cidSelecionada != null)
            {
                float cx = (float)(cidSelecionada.X * pbMapa.Width);
                float cy = (float)(cidSelecionada.Y * pbMapa.Height);
                g.FillEllipse(Brushes.Blue, cx - 3, cy - 3, 6, 6);
            }
        }

        private void DesenharArestas(Graphics g, Cidade origem, float x1, float y1)
        {
            if (origem.Ligacoes == null || origem.Ligacoes.EstaVazia) return;

            Pen caneta = new Pen(Color.LightGray);
            var node = origem.Ligacoes.Primeiro;

            while (node != null)
            {
                var lig = node.Info;
                var dest = cacheCidades.Find(c => c.Nome.Trim() == lig.Destino.Trim());

                if (dest != null)
                {
                    float x2 = (float)(dest.X * pbMapa.Width);
                    float y2 = (float)(dest.Y * pbMapa.Height);
                    g.DrawLine(caneta, x1, y1, x2, y2);
                }
                node = node.Prox;
            }
        }

        private void pbMapa_MouseClick(object sender, MouseEventArgs e)
        {
            udX.Value = (decimal)e.X / pbMapa.Width;
            udY.Value = (decimal)e.Y / pbMapa.Height;
            pbMapa.Invalidate();

            if (estadoAtual == EstadoApp.AguardandoPontoMapa)
            {
                if (MessageBox.Show($"Confirmar posição para '{txtNomeCidade.Text}'?", "Incluir", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    SalvarNovaCidade(new Cidade(txtNomeCidade.Text, (double)udX.Value, (double)udY.Value));
                }
            }
        }

        private void txtNomeCidade_Leave(object sender, EventArgs e)
        {
            if (estadoAtual == EstadoApp.AguardandoNome && !string.IsNullOrEmpty(txtNomeCidade.Text))
            {
                if (minhaArvore.Existe(new Cidade(txtNomeCidade.Text)))
                    MessageBox.Show("Já existe.");
                else
                {
                    estadoAtual = EstadoApp.AguardandoPontoMapa;
                    MessageBox.Show("Clique no mapa.");
                }
            }
        }

        private void btnBuscarCidade_Click(object sender, EventArgs e)
        {
            if (!minhaArvore.Existe(new Cidade(txtNomeCidade.Text)))
                MessageBox.Show("404 - Not Found");
            else
            {
                cidSelecionada = minhaArvore.Atual.Info;
                udX.Value = (decimal)cidSelecionada.X;
                udY.Value = (decimal)cidSelecionada.Y;
                AtualizarGridLigacoes();
                pbMapa.Invalidate();
            }
        }

        private void AtualizarGridLigacoes()
        {
            dgvLigacoes.Rows.Clear();
            if (cidSelecionada == null || cidSelecionada.Ligacoes == null) return;

            var no = cidSelecionada.Ligacoes.Primeiro;
            while (no != null)
            {
                dgvLigacoes.Rows.Add(no.Info.Destino, no.Info.Distancia);
                no = no.Prox;
            }
        }

        private void CarregarComboDestinos()
        {
            cbxCidadeDestino.Items.Clear();
            foreach (var c in cacheCidades)
                cbxCidadeDestino.Items.Add(c.Nome.Trim());
        }

        private void btnAlterarCidade_Click(object sender, EventArgs e)
        {
            if (cidSelecionada != null)
            {
                cidSelecionada.X = (double)udX.Value;
                cidSelecionada.Y = (double)udY.Value;
                MessageBox.Show("Atualizado.");
                pbMapa.Invalidate();
            }
            else
                MessageBox.Show("Selecione antes.");
        }

        private void btnExcluirCidade_Click(object sender, EventArgs e)
        {
            if (cidSelecionada != null)
            {
                if (!cidSelecionada.Ligacoes.EstaVazia)
                    MessageBox.Show("Remova as conexões antes de excluir a cidade.");
                else if (MessageBox.Show("Confirmar exclusão?", "Apagar", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    minhaArvore.Excluir(cidSelecionada);
                    cidSelecionada = null;
                    RecarregarCache();
                    SincronizarGrafo();
                    MessageBox.Show("Feito.");
                    pbMapa.Invalidate();
                    pnlArvore.Invalidate();
                }
            }
        }

        private void btnIncluirCaminho_Click(object sender, EventArgs e)
        {
            if (cidSelecionada != null && !string.IsNullOrEmpty(txtNovoDestino.Text))
            {
                if (minhaArvore.Existe(new Cidade(txtNovoDestino.Text)))
                {
                    int d = (int)numericUpDown1.Value;
                    string nmDest = txtNovoDestino.Text;

                    cidSelecionada.InserirLigacao(cidSelecionada.Nome, nmDest, d);
                    minhaArvore.Atual.Info.InserirLigacao(nmDest, cidSelecionada.Nome, d);

                    SincronizarGrafo();
                    AtualizarGridLigacoes();
                    pbMapa.Invalidate();
                }
                else
                    MessageBox.Show("Destino inválido.");
            }
        }

        private void btnExcluirCaminho_Click(object sender, EventArgs e)
        {
            if (dgvLigacoes.CurrentRow != null && cidSelecionada != null)
            {
                string target = dgvLigacoes.CurrentRow.Cells[0].Value.ToString();
                if (MessageBox.Show("Apagar rota?", "Confirmação", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    cidSelecionada.RemoverLigacao(cidSelecionada.Nome, target);
                    if (minhaArvore.Existe(new Cidade(target)))
                        minhaArvore.Atual.Info.RemoverLigacao(target, cidSelecionada.Nome);

                    SincronizarGrafo();
                    AtualizarGridLigacoes();
                    pbMapa.Invalidate();
                }
            }
        }

        private void btnBuscarCaminho_Click(object sender, EventArgs e)
        {
            if (cbxCidadeDestino.SelectedItem != null && cidSelecionada != null)
            {
                string alvo = cbxCidadeDestino.SelectedItem.ToString();
                int idxInicio = cacheCidades.IndexOf(cidSelecionada);
                int idxFim = cacheCidades.FindIndex(c => c.Nome.Trim() == alvo.Trim());

                if (idxInicio >= 0 && idxFim >= 0)
                    ProcessarDijkstra(idxInicio, idxFim);
            }
        }

        public void ProcessarDijkstra(int inicio, int fim)
        {
            int n = cacheCidades.Count;

            distanciasMinimas = new int[n];
            cidadesPai = new string[n];
            visitados = new bool[n];

            // Inicialização
            for (int i = 0; i < n; i++)
            {
                distanciasMinimas[i] = matrizAdjacencia[inicio, i];
                cidadesPai[i] = cacheCidades[inicio].Nome;
                visitados[i] = false;
            }

            visitados[inicio] = true;
            distanciasMinimas[inicio] = 0;

            // Relaxamento
            for (int i = 0; i < n; i++)
            {
                int u = ObterVerticeMaisProximo();
                if (u == -1) break;

                visitados[u] = true;

                for (int v = 0; v < n; v++)
                {
                    if (!visitados[v] && matrizAdjacencia[u, v] != INF)
                    {
                        int novaDist = distanciasMinimas[u] + matrizAdjacencia[u, v];
                        if (novaDist < distanciasMinimas[v])
                        {
                            distanciasMinimas[v] = novaDist;
                            cidadesPai[v] = cacheCidades[u].Nome;
                        }
                    }
                }
            }

            ExibirResultadoDijkstra(inicio, fim);
        }

        private int ObterVerticeMaisProximo()
        {
            int min = INF;
            int idx = -1;
            for (int i = 0; i < cacheCidades.Count; i++)
            {
                if (!visitados[i] && distanciasMinimas[i] < min)
                {
                    min = distanciasMinimas[i];
                    idx = i;
                }
            }
            return idx;
        }

        private void ExibirResultadoDijkstra(int inicio, int fim)
        {
            dgvRotas.Rows.Clear();

            if (distanciasMinimas[fim] == INF)
            {
                MessageBox.Show("Inacessível.");
                return;
            }

            var stack = new Stack<string>();
            int currIdx = fim;

            // Reconstrói o caminho de trás pra frente
            while (currIdx != inicio)
            {
                stack.Push(cacheCidades[currIdx].Nome);
                string nomePai = cidadesPai[currIdx];
                currIdx = cacheCidades.FindIndex(c => c.Nome == nomePai);
            }
            stack.Push(cacheCidades[inicio].Nome);

            while (stack.Count > 0)
            {
                dgvRotas.Rows.Add(stack.Pop(), "");
            }
            lbDistanciaTotal.Text = "Total: " + distanciasMinimas[fim] + " Km";
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (dlgAbrir.ShowDialog() == DialogResult.OK)
            {
                minhaArvore.GravarArquivoDeRegistros(dlgAbrir.FileName);
                if (dlgAbrir.ShowDialog() == DialogResult.OK)
                {
                    var w = new StreamWriter(dlgAbrir.FileName);
                    foreach (var c in cacheCidades)
                    {
                        var lista = c.Ligacoes;
                        if (lista != null)
                        {
                            var no = lista.Primeiro;
                            while (no != null)
                            {
                                w.WriteLine($"{c.Nome.Trim()};{no.Info.Destino.Trim()};{no.Info.Distancia}");
                                no = no.Prox;
                            }
                        }
                    }
                    w.Close();
                }
            }
        }

        private void btnBuscarCaminho_Click_1(object sender, EventArgs e)
        {

        }
    }
}