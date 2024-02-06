SELECT 
	ai_1 as "name", 
	SUM(ai_1_sym_win) as "sym_win",
	SUM(ai_1_win) as "win",
	SUM(repeats) as "iterations",
	MAX(unit_count) as "unit_count",
	SUM(ai_1_sym_win) * 1.0 / SUM(repeats) as "ratio"
	FROM
	((SELECT 
		ai_1, 
		ai_1_sym_win, 
	  	ai_1_win,
		ai_1_bs_unit_count + ai_1_d_unit_count as unit_count,
	  	repeats
	  FROM battles)
	UNION ALL
	(SELECT 
	 	ai_2 as "ai_1",
	 	ai_2_sym_win as "ai_1_sym_win", 
	 	ai_2_win as "ai_1_win",
	 	ai_1_bs_unit_count + ai_1_d_unit_count as unit_count,
	 	repeats
	 FROM battles)) as ALL_RESULTS
WHERE unit_count = 16
GROUP BY ai_1
ORDER BY ratio DESC
