drop table if exists program_outputs;
drop table if exists program_executions;
drop table if exists programs;

create table programs (
    id serial primary key,
    name text not null unique,
    docker_image text not null unique,
    created_at timestamp with time zone default CURRENT_TIMESTAMP not null
);

create table program_executions (
    id serial primary key,
    program_id serial references programs(id) not null,
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

-- create or replace function trigger_manager_application(docker_image text)
-- returns int
-- language plpgsql
-- as
-- $$
-- declare
-- $$;

-- create or replace trigger program_trigger 
-- after insert
-- on program_executions
-- execute 