# Generated manually to handle ManyToManyField change from through to direct

from django.db import migrations, models


class Migration(migrations.Migration):

    dependencies = [
        ('albums', '0001_initial'),
        ('media_app', '0002_media_ai_classifications_alter_media_tags'),
    ]

    operations = [
        # Krok 1: Usuń stare pole ManyToMany z through
        migrations.RemoveField(
            model_name='album',
            name='media',
        ),
        # Krok 2: Usuń model AlbumMedia (tabelę through)
        migrations.DeleteModel(
            name='AlbumMedia',
        ),
        # Krok 3: Dodaj nowe pole ManyToMany bez through
        migrations.AddField(
            model_name='album',
            name='media',
            field=models.ManyToManyField(blank=True, related_name='albums', to='media_app.media'),
        ),
    ]
