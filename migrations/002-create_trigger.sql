create or replace function trigger_manager_application(docker_image text)
returns int
language plpgsql
as
$$
declare
$$;

create or replace trigger program_trigger 
after insert
on program_executions
execute 