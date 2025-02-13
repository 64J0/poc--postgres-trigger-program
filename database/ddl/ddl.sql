drop table if exists program_outputs;
drop table if exists program_executions;
drop table if exists programs;

create table programs (
    docker_image text primary key,
    name text not null unique,
    created_at timestamp with time zone default CURRENT_TIMESTAMP not null
);

create table program_executions (
    id serial primary key,
    program_name text references programs(name) not null,
    program_input text not null,
    created_at timestamp with time zone default CURRENT_TIMESTAMP not null
);

create table program_outputs (
    id serial primary key,
    execution_id serial references program_executions(id) not null,
    pull_success bool default null,
    stdout_log text not null,
    stderr_log text not null
);

create or replace function trigger_manager_application()
returns trigger as
$$
begin
    PERFORM(
        SELECT pg_notify('program_manager_channel', NEW.id::text)
    );

    RETURN null;
end;
$$ language plpgsql;

create or replace trigger program_trigger_for_manager_svc
after insert on program_executions
execute function trigger_manager_application();
