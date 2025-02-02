﻿using ConroleAcesso.Entidades;
using ConroleAcesso.Extensions;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConroleAcesso.Dao
{
    public class PilotoDao : DaoBase
    {
        public async Task InserirPilotos(List<Piloto> pilotos)
        {
            if (!pilotos.Any())
                return;

            var check = "if (not exists(select 1 from Pilotos where IdPiloto = {0}))\n";
            var insert = "insert Pilotos (IdPiloto, Nome, AnoNacimiento, IdPlaneta) values({0}, '{1}', '{2}', {3});\n";
            var comandos = pilotos.Select(piloto => string.Format(check, piloto.IdPiloto) + string.Format(insert, piloto.IdPiloto, piloto.Nome, piloto.AnoNacimiento, piloto.IdPlaneta));

            await Insert(string.Join('\n', comandos));
        }

        public async Task RegistrarInicioViagem(int idPiloto, int idNave)
        {
            StringBuilder comando = new StringBuilder();
            comando.AppendLine($"if (not exists(select 1 from HistoricoViajes where IdPiloto = {idPiloto} and FechaRetorno is null))");
            comando.AppendLine($"begin");
            comando.AppendLine($"   insert HistoricoViajes (IdNave, IdPiloto, FechaSalida) values({idNave}, {idPiloto}, GetDate());");
            comando.AppendLine($"end");

            await Insert(string.Join('\n', comando.ToString()));
        }

        public async Task RegistrarFimViagem(int idPiloto, int idNave)
        {
            StringBuilder comando = new StringBuilder();
            comando.AppendLine($"update HistoricoViajes set FechaRetorno = GetDate() where IdPiloto = {idPiloto} and IdNave = {idNave} and FechaRetorno is null;");

            await Insert(string.Join('\n', comando.ToString()));
        }

        public async Task InserirPilotosNaves(List<Piloto> pilotos)
        {
            if (!pilotos.Any())
                return;

            var check = "if (not exists(select 1 from PilotosNaves where IdPiloto = {0} and IdNave = {1}))\n";
            var insert = "insert PilotosNaves (IdPiloto, IdNave) values({0}, {1});\n";
            var comandos = pilotos.SelectMany(piloto => piloto.Naves.Select(nave => string.Format(check, piloto.IdPiloto, nave.IdNave) + string.Format(insert, piloto.IdPiloto, nave.IdNave)));

            await Insert(string.Join('\n', comandos));
        }

        public async Task<bool> PilotoEstaViajando(int idPiloto)
        {
            bool viajando = false;

            var comando = $"select convert(bit, case when count(FechaSalida) <> count(FechaRetorno) then 1 else 0 end) Viajando from HistoricoViajes where IdPiloto = {idPiloto}";

            await Select(comando, resultadoSQL =>
            {
                while (resultadoSQL.Read())
                {
                    viajando = resultadoSQL.GetValueOrDefault<bool>("Viajando");
                }
            });

            return viajando;
        }

        public async Task<Piloto> ObterPorId(int idPiloto)
        {
            Piloto piloto = null;
            var comando = @$"
                                select  t1.IdPiloto,
                                        t1.Nome,
                                        t1.AnoNacimiento,
                                        t2.IdPlaneta,
                                        t2.Rotacion,
                                        t2.Orbita,
                                        t2.Diametro,
                                        t2.Clima,
                                        t2.Populacion
                                from    Pilotos t1
                                inner   join Planetas t2
                                on      t1.IdPlaneta = t2.IdPlaneta
                                where   IdPiloto = {idPiloto}";

            await Select(comando, resultadoSQL =>
            {
                while (resultadoSQL.Read())
                {
                    piloto = new Piloto
                    {
                        IdPiloto = resultadoSQL.GetValueOrDefault<int>("IdPiloto"),
                        Nome = resultadoSQL.GetValueOrDefault<string>("Nome"),
                        AnoNacimiento = resultadoSQL.GetValueOrDefault<string>("AnoNacimiento"),
                        IdPlaneta = resultadoSQL.GetValueOrDefault<int>("IdPlaneta"),
                        Planeta = new Planeta
                        {
                            IdPlaneta = resultadoSQL.GetValueOrDefault<int>("IdPlaneta"),
                            Nome = resultadoSQL.GetValueOrDefault<string>("Nome"),
                            Rotacion = resultadoSQL.GetValueOrDefault<double>("Rotacion"),
                            Orbita = resultadoSQL.GetValueOrDefault<double>("Orbita"),
                            Diametro = resultadoSQL.GetValueOrDefault<double>("Diametro"),
                            Clima = resultadoSQL.GetValueOrDefault<string>("Clima"),
                            Populacion = resultadoSQL.GetValueOrDefault<int>("Populacion")
                        }
                    };
                }
            });

            piloto.Naves = new List<Nave>();
            comando = @$"
                                select  t2.*
                                from    PilotosNaves t1
                                inner join Naves t2
                                on      t1.IdNave = t2.IdNave
                                where   t1.FlagAutorizado = 1
                                and     t1.IdPiloto = {idPiloto}";

            await Select(comando, resultadoSQL =>
            {
                while (resultadoSQL.Read())
                {
                    piloto.Naves.Add(new Nave
                    {
                        IdNave = resultadoSQL.GetValueOrDefault<int>("IdNave"),
                        Nombre = resultadoSQL.GetValueOrDefault<string>("Nombre"),
                        Modelo = resultadoSQL.GetValueOrDefault<string>("Modelo"),
                        Pasajeros = resultadoSQL.GetValueOrDefault<int>("Pasajeros"),
                        Carga = resultadoSQL.GetValueOrDefault<double>("Carga"),
                        Clase = resultadoSQL.GetValueOrDefault<string>("Clase")
                    });
                }
            });

            return piloto;
        }

        public async Task<List<Piloto>> ObterPorNomeLike(string nome)
        {
            var pilotos = new List<Piloto>();
            var comando = $"select * from Pilotos where nome like '%{nome.Replace(' ', '%')}%'";

            await Select(comando, resultadoSQL =>
            {
                while (resultadoSQL.Read())
                {
                    pilotos.Add(new Piloto
                    {
                        IdPiloto = resultadoSQL.GetValueOrDefault<int>("IdPiloto"),
                        Nome = resultadoSQL.GetValueOrDefault<string>("Nome")
                    });
                }
            });

            return pilotos;
        }
    }
}
