﻿using System;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using de.fearvel.net.SQL.Connector;

namespace de.fearvel.net.FnLog.Database
{
    public class LocalLogger
    {
        private SqliteConnector _connection;
        internal Guid Guid { private set; get; }
        internal Guid GetGuid()
        {
            _connection.Query("Select Val from Directory where Identifier = 'GUID';", out DataTable dt);
            if (dt.Rows.Count == 0 || !Guid.TryParse(dt.Rows[0].Field<string>("Val"), out var res))
                return Guid.Empty;
            return res;
        }

        public LocalLogger( string fileName) : this( fileName, "") { }

        public LocalLogger( string fileName, string encKey)
        {
            _connection = new SqliteConnector(fileName, encKey);
            CreateTables();
            Guid = GetGuid();
        }

        private void CreateTables()
        {
            CreateInformationTable();
            CreateErrorTable();
        }

        private void CreateInformationTable()
        {
            _connection.NonQuery("CREATE TABLE IF NOT EXISTS Directory" +
                     " (Identifier varchar(200),val Text," +
                     " CONSTRAINT uq_Version_Identifier UNIQUE (Identifier));");
            _connection.Query("SELECT * FROM Directory", out DataTable dt);
            if (dt.Rows.Count != 0) return;
            _connection.NonQuery("INSERT INTO Directory (Identifier,val) VALUES ('LoggerVersion'," +
                     "'" + FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).ProductVersion + "');");
            _connection.NonQuery("INSERT INTO Directory (Identifier,val) VALUES ('GUID','" + Guid.NewGuid().ToString() + "');");
        }

        private void CreateErrorTable()
        {
            _connection.NonQuery("CREATE TABLE IF NOT EXISTS Log" +
                    " (id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT," +
                    " LogType int NOT NULL," +
                    " Title varchar(200) NOT NULL DEFAULT ''," +
                    " Description text NOT NULL DEFAULT ''," +
                    " DateTimeOfIncident Timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP," +
                    " Reported boolean NOT NULL DEFAULT 0);");
            _connection.NonQuery("CREATE TRIGGER IF NOT EXISTS removeOldRuntimeInfoOnInsert After INSERT ON Log" +
                    " BEGIN Delete from Log where DateTimeOfIncident <= date('now','-30 day'); END");
            _connection.NonQuery("CREATE TRIGGER IF NOT EXISTS removeReportedRuntimeInfo After INSERT ON Log" +
                                 " BEGIN Delete from Log where DateTimeOfIncident <= date('now','-3 day') and Reported =1; END");
        }

        public void AddLog(FnLog.LogType logType, string title, string description, bool reported)
        {
            using (var command = new SQLiteCommand(
                "Insert Into Log(LogType,Title,Description, Reported) values (@LogType,@Title,@Description, @Reported)"))
            {
                command.Parameters.AddWithValue("@LogType", logType);
                command.Parameters.AddWithValue("@Title", title);
                command.Parameters.AddWithValue("@Description", description);
                command.Parameters.AddWithValue("@Reported", BoolToInt(reported));
                command.Prepare();
                _connection.NonQuery(command);
            }
        }

        private int BoolToInt(bool b) => b ? 1 : 0;

        public void AddLog(FnLog.LogType logType, string title, string description) => 
            AddLog(logType, title, description, false);


        public DataTable GetErrorsAndWarnings()
        {
            _connection.Query("Select log.LogType, log.Title, log.Description, log.DateTimeOfIncident, version.Val as Version, guid.Val as GUID from" +
                                     " Log log left join Directory version left join Directory guid " +
                                     "where version.Identifier = 'LoggerVersion' and guid.Identifier = 'GUID' and log.Reported = 0 and ( LogType = "
                                     + (int)FnLog.LogType.CriticalError + " or LogType = "
                                     + (int)FnLog.LogType.Error + " or LogType = "
                                     + (int)FnLog.LogType.Warning + " or LogType = "
                                     + (int)FnLog.LogType.Notice + ");", out DataTable dt);
            _connection.NonQuery("Update Log set Reported = 1 where Reported = 0 and ( LogType = "
                              + (int)FnLog.LogType.CriticalError + " or LogType = "
                              + (int)FnLog.LogType.Error + " or LogType = "
                              + (int)FnLog.LogType.Warning + " or LogType = "
                              + (int)FnLog.LogType.Notice + ");");
            return dt;

        }

        public DataTable GetLog(FnLog.LogType t)
        {
            _connection.Query("Select * from Log where LogType = '" + t.ToString() + "';", out DataTable dt);
            return dt;
        }

        internal DataTable DumpLog()
        {
            _connection.Query("Select * from Log;", out DataTable dt);
            return dt;
        }
    }
}