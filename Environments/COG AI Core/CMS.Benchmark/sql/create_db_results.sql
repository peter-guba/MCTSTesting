CREATE DATABASE cog_results
    WITH 
    OWNER = postgres
    ENCODING = 'UTF8'
    LC_COLLATE = 'English_United States.1252'
    LC_CTYPE = 'English_United States.1252'
    TABLESPACE = pg_default
    CONNECTION LIMIT = -1
	TEMPLATE = template0;

GRANT CREATE, CONNECT ON DATABASE cog_results TO postgres;
GRANT TEMPORARY ON DATABASE cog_results TO postgres WITH GRANT OPTION;

GRANT TEMPORARY, CONNECT ON DATABASE cog_results TO PUBLIC;
