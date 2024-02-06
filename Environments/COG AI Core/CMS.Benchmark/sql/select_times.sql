SELECT 
	ai_1 as "name",
	MAX(ai_1_max_round_time) as max_max_time,
	AVG(ai_1_max_round_time) as avg_max_time,
	PERCENTILE_DISC(0.9) WITHIN GROUP(ORDER BY ai_1_max_round_time) as p90_max_time,
	AVG(ai_1_avg_round_time) as avg_time,
	PERCENTILE_DISC(0.5) WITHIN GROUP(ORDER BY ai_1_median_round_time) as median_time,
	MAX(unit_count),
	SUM(repeats) + COUNT(ai_1) as "repeats"
	FROM
	((SELECT ai_1, 
	  ai_1_max_round_time, 
	  ai_1_avg_round_time, 
	  ai_1_median_round_time, 
	  ai_1_bs_unit_count + ai_1_d_unit_count as "unit_count",
	  repeats
	  FROM battles)
	UNION
	(SELECT ai_2 as "ai_1", 
	 ai_2_max_round_time as "ai_1_max_round_time", 
	 ai_2_avg_round_time as "ai_1_avg_round_time",
	 ai_2_median_round_time as ai_1_median_round_time,
	 ai_1_bs_unit_count + ai_1_d_unit_count as "unit_count",
	 repeats
	 FROM battles)	 
	) as all_times
--WHERE ai_1 = 'MCTS_'
WHERE unit_count = 3 AND ai_1 LIKE '%PGS%'
GROUP BY ai_1
ORDER BY ai_1 DESC