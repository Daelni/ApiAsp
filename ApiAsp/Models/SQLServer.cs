using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
//using Microsoft.Extensions.Configuration;

namespace ApiAsp.Entities
{
    public class BaseDatos
    {

        /// <summary>
        /// Propiedad privada Conexion
        /// </summary>
        /// <value>
        /// Se utiliza para hacer referencia a la base de datos
        /// </value>
        private SqlConnection Conexion;

        /// --------------------------------------------------------------------------------
        /// <summary>
        /// Metodo que se utiliza para abrir una conexion al sevidor de base de datos
        /// </summary>
        /// --------------------------------------------------------------------------------
        public void AbrirConexion()
        {
            // obtener cadena de conexion del archivo conexion.config
            string RutaArchivoConexion = AppDomain.CurrentDomain.BaseDirectory.ToString() + "/" + "conexion.config";
            string CadenaConexion = File.ReadLines(RutaArchivoConexion).First().ToString();
            Conexion = new SqlConnection();
            Conexion.ConnectionString = CadenaConexion;

            Conexion.Open();


        }


        /// --------------------------------------------------------------------------------
        /// <summary>
        /// Metodo para cerrar una conexion al sevidor de base de datos
        /// </summary>
        /// --------------------------------------------------------------------------------
        public void CerrarConexion()
        {
            if ((Conexion == null) || (Conexion.State != ConnectionState.Closed))
            {
                Conexion.Close();
                Conexion = null;
            }
        }

        /// --------------------------------------------------------------------------------
        /// <summary>
        /// Metodo para ejecutar un comando con una sentencia Sql en formato de texto plano
        /// </summary>
        /// <param name="SentenciaSql">Sentencia Sql</param>
        /// <returns>True/False</returns>
        /// --------------------------------------------------------------------------------
        public bool EjecutarComando(string SentenciaSql)// no tiene referencias
        {
            AbrirConexion();
            SqlTransaction Transaccion = Conexion.BeginTransaction();
            try
            {
                SqlCommand Comando;

                Comando = new SqlCommand()
                {
                    Transaction = Transaccion,
                    Connection = Conexion,
                    CommandText = SentenciaSql
                };
                Comando.ExecuteNonQuery();
                Transaccion.Commit();
                Comando = null;
                CerrarConexion();
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Transaccion.Rollback();
                CerrarConexion();
                return false;
            }
        }

        /// --------------------------------------------------------------------------------
        /// <summary>
        /// metodo para ejecutar un comando con una sentencia parametrizada
        /// </summary>
        /// <param name="SentenciaSql">Sentencia Sql</param>
        /// <param name="Parametros">Lista de parametros Sql</param>
        /// <returns></returns>
        /// --------------------------------------------------------------------------------
        public bool EjecutarComando(string SentenciaSql, List<SqlParameter> Parametros)
        {
            AbrirConexion();
            SqlTransaction Transaccion = Conexion.BeginTransaction();

            SqlCommand Comando = new SqlCommand()
            {
                Transaction = Transaccion,
                Connection = Conexion,
                CommandText = SentenciaSql
            };

            // loop para generar lista parametrizada en el comando
            foreach (SqlParameter param in Parametros)
            {
                Comando.Parameters.AddWithValue(param.ParameterName, param.Value);
            }
            try
            {
                Comando.ExecuteNonQuery();
                Transaccion.Commit();
                Comando = null;
                CerrarConexion();
                return true;
            }
            catch (Exception e)
            {

                string query = Comando.CommandText;//obtenemos el conmando sql
                foreach (SqlParameter p in Comando.Parameters)
                {
                    query = query.Replace(p.ParameterName, p.Value.ToString());
                }//insertamos los parametros para remplazar las variables


                Transaccion.Rollback();
                CerrarConexion();
                return false;
            }

        }

        /// --------------------------------------------------------------------------------
        /// <summary>
        /// Metodo para ejecutar un comando con una sentencia parametrizada y 
        /// retornar el ultimo Id insertado
        /// </summary>
        /// <param name="SentenciaSql">Sentencia Sql</param>
        /// <param name="Parametros">Lista de parametros Sql</param>
        /// <returns>Valor del ultimo Id generado</returns>
        /// --------------------------------------------------------------------------------
        public dynamic EjecutarComandoObtenerUltimoId(string SentenciaSql, List<SqlParameter> Parametros)
        {
            dynamic UltimoId;
            SqlCommand Comando;

            AbrirConexion();
            SqlTransaction transaction = Conexion.BeginTransaction();

            Comando = new SqlCommand()
            {
                Transaction = transaction,
                Connection = Conexion,
                CommandText = SentenciaSql + " select @UltimoId = scope_identity();"
            };
            try
            {
                // loop para generar lista parametrizada en el comando
                foreach (SqlParameter param in Parametros)
                {
                    if (param.Value == null)
                    {
                        param.Value = DBNull.Value;
                    }
                    Comando.Parameters.AddWithValue(param.ParameterName, param.Value);
                }

                Comando.Parameters.Add(new SqlParameter("@UltimoId", SqlDbType.BigInt));
                Comando.Parameters["@UltimoId"].Direction = ParameterDirection.Output;
                Comando.ExecuteNonQuery();
                transaction.Commit();

                UltimoId = Comando.Parameters["@UltimoId"].Value;

                Comando = null;

                CerrarConexion();

                return UltimoId;
            }
            catch (Exception e)
            {
                string query = Comando.CommandText;//obtenemos el conmando sql
                foreach (SqlParameter p in Comando.Parameters)
                {
                    query = query.Replace(p.ParameterName, p.Value.ToString());
                }//insertamos los parametros para remplazar las variables


                transaction.Rollback();
                CerrarConexion();
                return false;
            }
        }

        /// --------------------------------------------------------------------------------
        /// <summary>
        /// Metodo para obtener un objeto DataSet con una sentencia Sql en formato de texto plano
        /// </summary>
        /// <param name="SentenciaSql">Sentencia Sql</param>
        /// <returns>Objeto DataSet</returns>
        /// --------------------------------------------------------------------------------
        public DataSet ObtenerDataSet(string SentenciaSql)// no tiene referencias
        {
            SqlDataAdapter Adaptador;
            DataSet DataSet;

            AbrirConexion();
            Adaptador = new SqlDataAdapter(SentenciaSql, Conexion);
            DataSet = new DataSet();

            try
            {
                Adaptador.Fill(DataSet);
                Adaptador = null;

                if (DataSet.Tables.Count < 0)
                {
                    DataSet = null;
                }
                CerrarConexion();

                return DataSet;
            }
            catch (Exception e)
            {
                string query = Adaptador.SelectCommand.CommandText;//obtenemos el conmando sql
                foreach (SqlParameter p in Adaptador.SelectCommand.Parameters)
                {
                    query = query.Replace(p.ParameterName, p.Value.ToString());
                }//insertamos los parametros para remplazar las variables


                CerrarConexion();
                return null;
            }
        }

        /// --------------------------------------------------------------------------------
        /// <summary>
        /// Metodo para obtener un objeto DataSet con una sentencia Sql parametrizada
        /// </summary>
        /// <param name="SentenciaSql">Sentencia Sql</param>
        /// <param name="Parametros">Lista de parametros Sql</param>
        /// <returns>Objeto DataSet</returns>
        /// --------------------------------------------------------------------------------
        public DataSet ObtenerDataSet(string SentenciaSql, List<SqlParameter> Parametros)// no tiene referencias
        {
            SqlDataAdapter Adaptador;
            DataSet DataSet;

            AbrirConexion();
            Adaptador = new SqlDataAdapter(SentenciaSql, Conexion);
            foreach (SqlParameter param in Parametros)
            {
                Adaptador.SelectCommand.Parameters.AddWithValue(param.ParameterName, param.Value);
            }

            try
            {
                DataSet = new DataSet();
                Adaptador.Fill(DataSet);
                Adaptador = null;

                if (DataSet.Tables.Count < 0)
                {
                    DataSet = null;
                }
                CerrarConexion();

                return DataSet;
            }
            catch (Exception e)
            {
                string query = Adaptador.SelectCommand.CommandText;//obtenemos el conmando sql
                foreach (SqlParameter p in Adaptador.SelectCommand.Parameters)
                {
                    query = query.Replace(p.ParameterName, p.Value.ToString());
                }//insertamos los parametros para remplazar las variables


                CerrarConexion();
                return null;
            }
        }

        /// --------------------------------------------------------------------------------
        /// <summary>
        /// Metodo para obtener un objeto DataTable con una sentencia de texto plano
        /// </summary>
        /// <param name="SentenciaSql">Sentencia Sql</param>
        /// <returns>Objeto DataTable</returns>
        /// --------------------------------------------------------------------------------
        public DataTable ObtenerDataTable(string SentenciaSql)
        {

            SqlDataAdapter Adaptador;
            DataSet DataSet;
            DataTable DataTable;

            AbrirConexion();
            Adaptador = new SqlDataAdapter(SentenciaSql, Conexion);
            DataSet = new DataSet();

            try
            {
                Adaptador.Fill(DataSet);
                Adaptador = null;

                if (DataSet.Tables.Count > 0)
                {
                    DataTable = new DataTable();
                    DataTable = DataSet.Tables[0];
                }
                else
                {
                    DataTable = null;
                }
                CerrarConexion();

                return DataTable;
            }
            catch (Exception e)
            {
                string query = Adaptador.SelectCommand.CommandText;//obtenemos el conmando sql


                CerrarConexion();
                return null;
            }
        }

        /// --------------------------------------------------------------------------------
        /// <summary>
        /// metodo para obtener un datatable con una sentencia parametrizada
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="Parametros"></param>
        /// <returns></returns>
        /// --------------------------------------------------------------------------------
        public DataTable ObtenerDataTable(string SentenciaSql, List<SqlParameter> Parametros)
        {
            SqlDataAdapter Adaptador;
            DataSet DataSet;
            DataTable DataTable;

            AbrirConexion();
            Adaptador = new SqlDataAdapter(SentenciaSql, Conexion);
            Adaptador.SelectCommand.CommandTimeout = 300; // se descubrio que en consultas de gran cantidad de tiempo generaba un error la solucion fue incrementar esta variable
            foreach (SqlParameter param in Parametros)
            {
                Adaptador.SelectCommand.Parameters.AddWithValue(param.ParameterName, param.Value);
            }

            try
            {
                DataSet = new DataSet();
                Adaptador.Fill(DataSet);
                Adaptador = null;

                if (DataSet.Tables.Count > 0)
                {
                    DataTable = new DataTable();
                    DataTable = DataSet.Tables[0];
                }
                else
                {
                    DataTable = null;
                }
                CerrarConexion();

                return DataTable;
            }
            catch (Exception e)
            {
                string query = Adaptador.SelectCommand.CommandText;//obtenemos el conmando sql
                foreach (SqlParameter p in Adaptador.SelectCommand.Parameters)
                {
                    query = query.Replace(p.ParameterName, p.Value.ToString());
                }//insertamos los parametros para remplazar las variables


                CerrarConexion();
                return null;
            }
        }
    }

    public class SqlClass
    {
        public SqlConnection con;
        // public IConfiguration Configuration { get; }

        public bool conectar()
        {
            con = new SqlConnection();

            string RutaArchivoConexion = AppDomain.CurrentDomain.BaseDirectory.ToString() + "/" + "conexion.config";
            string CadenaConexion = File.ReadLines(RutaArchivoConexion).First().ToString();
            con = new SqlConnection();
            con.ConnectionString = CadenaConexion;

            try
            {
                con.Open();
                con.Close();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
            //return true;
        }

        public SqlTransaction transaction_ = null;

        // Funcion Consultar datos con lista parametros  y lasta valores salida
        public List<parametro> funcStoreProcedureConsult(string storeProceduteName, ref DataTable dtConsulta, List<parametro> lstParametros, List<parametro> lstParametrosOutput)
        {
            SqlCommand command_ = new SqlCommand(storeProceduteName, con);
            List<SqlParameter> listParam = new List<SqlParameter>();
            command_.CommandType = CommandType.StoredProcedure;
            command_.Parameters.Clear();
            try
            {
                if (con.State == ConnectionState.Closed)
                {
                    con.Open();
                }

                for (int i = 0; i < lstParametros.Count; i++)
                {
                    command_.Parameters.AddWithValue(lstParametros[i].nombre, lstParametros[i].valor);
                }

                for (int i = 0; i < lstParametrosOutput.Count; i++)
                {
                    SqlDbType tipoSalida;
                    SqlParameter ValorRetorno;

                    switch (lstParametrosOutput[i].tipoValor)
                    {
                        case parametro.tipoValorSalida.int_:
                            tipoSalida = SqlDbType.Int;
                            ValorRetorno = new SqlParameter(lstParametrosOutput[i].nombre, tipoSalida);
                            listParam.Add(ValorRetorno);
                            ValorRetorno.Direction = ParameterDirection.Output;
                            command_.Parameters.Add(ValorRetorno);
                            break;
                        case parametro.tipoValorSalida.string_:
                            tipoSalida = SqlDbType.NVarChar;
                            ValorRetorno = new SqlParameter(lstParametrosOutput[i].nombre, tipoSalida, -1);
                            listParam.Add(ValorRetorno);
                            ValorRetorno.Direction = ParameterDirection.Output;
                            command_.Parameters.Add(ValorRetorno);
                            break;
                        default:
                            break;
                    }
                }

                DataSet ds = new DataSet();
                SqlDataAdapter da = new SqlDataAdapter(command_);
                da.Fill(dtConsulta);

                for (int i = 0; i < listParam.Count; i++)
                {
                    lstParametrosOutput[i].valor = listParam[i].Value.ToString();
                }
            }
            catch (Exception ex)
            {
                string query = command_.CommandText;//obtenemos el conmando sql

                foreach (SqlParameter p in command_.Parameters)
                {
                    var x = p.Value.ToString() == null ? "0" : p.Value.ToString();
                    query += p.ParameterName + " = " + x;
                    if (p.ParameterName == "@DatJson")
                    {
                        break;
                    }
                    else
                    {
                        query += " , ";
                    }
                }//insertamos los parametros para remplazar las variables
                //COSITECDLL.Excepcion.Registrar("Base de datos", ex.Message.ToString() + " " + ex.StackTrace.ToString(), query);
            }
            finally
            {
                con.Close();
            }

            return lstParametrosOutput;
        }
    }
}

public class parametro
{
    List<parametro> parametros = new List<parametro>();
    private String nombre_;
    public String nombre
    {
        get { return nombre_; }
        set { nombre_ = "@" + value; }
    }
    public String valor;

    public tipoValorSalida tipoValor;
    public enum tipoValorSalida
    {
        int_,
        string_
    };
}

public class respuestaCompuesta
{
    public int registrosTabla { get; set; }
    public int columnasAfectadas { get; set; }
    public bool error { get; set; }
    public string strError { get; set; }
    public string query { get; set; }

}

