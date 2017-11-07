# BackupExecutor
Windows service that manages and fulfills backups made to Oracle database instances.

## Modes of execution
The service can be configured in either `--master` or `--slave` mode

### Master
Master mode requires no further parameters. Usage is `BackupExecutorService --master`

This modes fulfills backups according to the strategies created for the local server, but also monitors the activity of the slave servers
through the logs

### Slave
Slave mode requires a minimum of one parameter. Usage is `BackupExecutorService --slave <ip address> [<port>]`

This modes fulfills backups according to the strategies found in the local server, the logs of this backups are sent back to the central server

## Installing

### Setting the user

In order to use the `system` user normally, the user must be unlocked to avoid any issues due to privileges.

``` sql
connect sys/root as sysdba;

ALTER USER SYSTEM IDENTIFIED BY MANAGER ACCOUNT UNLOCK;
```

### Creating a tablespace

Then, connect to the instance through `CONNECT SYSTEM/MANAGER` and execute the following SQLs

``` sql 
create tablespace
	backup_metadata
datafile
 	'C:\ORACLEXE\APP\ORACLE\ORADATA\XE\backup_metadata.dbf'
size
 	50m
AUTOEXTEND ON NEXT 10M MAXSIZE 200M;
```

### Creating the backup metadata tables

After creating the tablespace, create the tables used for storing all the backup metadata

``` sql
create table connection (
	conn_id varchar2(25) not null,
	conn_name varchar2(70) not null,
	database_instance varchar2(70) not null,
	ip varchar2(70) not null,
	port varchar2(6) default '1521' not null,
	alive number(1) default 1 not null,
	CONSTRAINT connection_pk PRIMARY KEY (conn_id)
) tablespace backup_metadata;

create table strategy (
	strategy_id varchar2(25) not null, -- UNIX timestamp
	connection varchar2(70) not null,
	priority varchar2(6) null, -- high, medium, low
	alive number(1) default 1 not null,
	CONSTRAINT strategy_pk PRIMARY KEY (strategy_id),
	CONSTRAINT strategy_fk FOREIGN KEY (connection) REFERENCES connection(conn_id)
) tablespace backup_metadata;

create table strategy_line (
	strategy_id varchar2(25) not null,
	line varchar2(255) null,
	CONSTRAINT strategy_line_fk FOREIGN KEY (strategy_id) REFERENCES strategy(strategy_id)
) tablespace backup_metadata;

create table frequency (
	strategy_id varchar2(25) not null, 
	day int not null, -- day of the week with sunday as 0, -1 if it's everyday
	hour int not null,
	minutes int not null,
	CONSTRAINT frequency_fk FOREIGN KEY (strategy_id) REFERENCES strategy(strategy_id)
) tablespace backup_metadata;

create table log (
	strategy_id varchar2(25) not null, 
	moment TIMESTAMP default CURRENT_TIMESTAMP not null,
	log CLOB,
	CONSTRAINT log_fk FOREIGN KEY (strategy_id) REFERENCES strategy(strategy_id)
) tablespace backup_metadata;

create table error (
	strategy_id varchar2(25) not null, 
	moment TIMESTAMP default CURRENT_TIMESTAMP not null,
	message varchar2(255),
	CONSTRAINT error_fk FOREIGN KEY (strategy_id) REFERENCES strategy(strategy_id)
) tablespace backup_metadata;
```

### Creating the required stored procedures and functions

After creating all this tables, some stored procedures and functions must be installed for the service to work

```sql
CREATE OR REPLACE FUNCTION scheduled_backups(d in int, h in int, m in int)
  RETURN SYS_REFCURSOR IS
  cr SYS_REFCURSOR;
  BEGIN
    OPEN cr FOR 
	select strategy_id from frequency
		where day = d
		and hour = h
		and minutes = m;
	RETURN CR;
  END;
/

CREATE OR REPLACE FUNCTION twelfth_backups(d in int, h in int, m_i in int, m_f in int)
  RETURN SYS_REFCURSOR IS
  cr SYS_REFCURSOR;
  BEGIN
    OPEN cr FOR 
	select strategy_id from frequency
		where day = d
		and hour = h
		and minutes between m_i and m_f;
	RETURN CR;
  END;
/

CREATE OR REPLACE FUNCTION logged_twelfth_backups(d in int, h in int, m_i in int, m_f in int)
  RETURN SYS_REFCURSOR IS
  cr SYS_REFCURSOR;
  BEGIN
    OPEN cr FOR 
		select strategy_id from log
		where to_char(moment, 'D')-1 = d
		and to_char(moment, 'HH') = h
		and to_char(moment, 'MM') between m_i and m_f;
	RETURN CR;
  END;
/

CREATE OR REPLACE FUNCTION strategy_instructions(name in varchar2)
  RETURN SYS_REFCURSOR IS
  cr SYS_REFCURSOR;
  BEGIN
    OPEN cr FOR 
	select line from strategy_line 
		where strategy_id = name;
	RETURN CR;
  END;
/

CREATE OR REPLACE PROCEDURE insert_log(name in varchar2, log in clob)
  IS
  BEGIN
    insert into log(strategy_id, log) values (name, log);
	COMMIT;

	EXCEPTION
     WHEN OTHERS THEN ROLLBACK;
  END;
/

CREATE OR REPLACE PROCEDURE insert_error(name in varchar2, msg in varchar2)
  IS
  BEGIN
    insert into error(strategy_id, message) values (name, msg);
	COMMIT;

	EXCEPTION
     WHEN OTHERS THEN ROLLBACK;
  END;
/
```

### Test inserts

Finally, the user cand find above some test inserts in order to better understand the metadata structure

```sql

-- Sample connection
insert into connection(conn_id, conn_name, database_instance, ip) values ('C_1509986176','local','XE','127.0.0.1');

-- Sample strategies
insert into strategy(strategy_id, connection, priority) values ('EST_1509986176','C_1509986176','MEDIUM');
insert into strategy(strategy_id, connection, priority) values ('EST_1509986180','C_1509986176','MEDIUM');
insert into strategy(strategy_id, connection, priority) values ('EST_1509986184','C_1509986176','MEDIUM');
insert into strategy(strategy_id, connection, priority) values ('EST_1509986188','C_1509986176','MEDIUM');
insert into strategy(strategy_id, connection, priority) values ('EST_1509986192','C_1509986176','MEDIUM');

-- Sample Users tablespace backup
insert into strategy_line(strategy_id, line) values ('EST_1509986176','crosscheck archivelog all;');
insert into strategy_line(strategy_id, line) values ('EST_1509986176','run{');
insert into strategy_line(strategy_id, line) values ('EST_1509986176','SQL "alter system switch logfile";');
insert into strategy_line(strategy_id, line) values ('EST_1509986176','backup tablespace users;');
insert into strategy_line(strategy_id, line) values ('EST_1509986176','}');

-- Sample erroneus backup
insert into strategy_line(strategy_id, line) values ('EST_1509986180','crosscheck archivelog all;');
insert into strategy_line(strategy_id, line) values ('EST_1509986180','run{;');
insert into strategy_line(strategy_id, line) values ('EST_1509986180','SQL "alter system switch logfile";');
insert into strategy_line(strategy_id, line) values ('EST_1509986180','backup tablespace users;');
insert into strategy_line(strategy_id, line) values ('EST_1509986180','}');

-- Sample frequencies
insert into frequency(strategy_id, day, hour, minutes) values ('EST_1509986176', 1, 12, 35);
insert into frequency(strategy_id, day, hour, minutes) values ('EST_1509986180', 1, 12, 35);
insert into frequency(strategy_id, day, hour, minutes) values ('EST_1509986184', 1, 12, 35);
insert into frequency(strategy_id, day, hour, minutes) values ('EST_1509986188', 1, 12, 39);
insert into frequency(strategy_id, day, hour, minutes) values ('EST_1509986192', 1, 12, 34);
```

