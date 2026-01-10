import redis
import json

redis_url = "redis://redis:6379/0"
redis_db = redis.from_url(redis_url)


def set_status(job_id, data):
    redis_db.set(job_id, json.dumps(data))


def get_status(job_id):
    raw = redis_db.get(job_id)
    if raw is None:
        return {"error": "unknown job_id"}
    return json.loads(raw)