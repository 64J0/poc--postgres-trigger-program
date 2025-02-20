drop table if exists program_outputs;
drop table if exists program_executions;
drop table if exists programs;

-- for simplicity, this is going to be used for running fsi scripts (F#
-- interactive scripts)
create table programs (
    id uuid primary key,
    program_name text not null,
    program_file_path text default null,
    created_at timestamp with time zone default CURRENT_TIMESTAMP not null
);

create table program_executions (
    id serial primary key,
    program_id text references programs(id) not null,
    program_input text not null,
    created_at timestamp with time zone default CURRENT_TIMESTAMP not null
);

create table program_outputs (
    id serial primary key,
    execution_id serial references program_executions(id) not null,
    execution_success bool not null default false,
    status_code int not null,
    stdout_log text not null,
    stderr_log text not null,
    created_at timestamp with time zone default CURRENT_TIMESTAMP not null
);

create or replace function trigger_manager_application()
returns trigger as
$$
begin
    PERFORM(
        SELECT pg_notify('program_manager_channel', new.id::text)
    );

    RETURN null;
end;
$$ language plpgsql;

-- https://stackoverflow.com/a/30884951
create or replace trigger program_trigger_for_manager_svc
after insert on program_executions
for each row
execute function trigger_manager_application();
