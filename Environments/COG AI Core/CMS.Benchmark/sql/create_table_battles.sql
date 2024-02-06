CREATE TABLE public.battles
(
    ai_1 character varying(256) COLLATE pg_catalog."default",
    ai_2 character varying(256) COLLATE pg_catalog."default",
    ai_1_bs_unit_count integer,
    ai_2_bs_unit_cout integer,
    ai_1_d_unit_count integer,
    ai_2_d_unit_count integer,
    repeats integer,
    ai_1_win integer,
    ai_2_win integer,
    ai_1_sym_win integer,
    ai_2_sym_win integer,
    ai_1_hull integer,
    ai_2_hull integer,
    min_rounds integer,
    median_rounds integer,
    max_rounds integer,
    avg_rounds real,
    ai_1_min_round_time real,
    ai_1_max_round_time real,
    ai_1_median_round_time real,
    ai_1_avg_round_time real,
    ai_2_min_round_time real,
    ai_2_max_round_time real,
    ai_2_median_round_time real,
    ai_2_avg_round_time real,
    id bigserial NOT NULL PRIMARY KEY
)
WITH (
    OIDS = FALSE
)
TABLESPACE pg_default;

ALTER TABLE public.battles
    OWNER to postgres;
    