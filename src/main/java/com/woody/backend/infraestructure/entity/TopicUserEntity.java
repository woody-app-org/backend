package com.woody.backend.infraestructure.entity;

import java.time.LocalDate;

import jakarta.persistence.EmbeddedId;
import jakarta.persistence.Entity;
import jakarta.persistence.ManyToOne;
import jakarta.persistence.MapsId;

@Entity
public class TopicUserEntity {
    @EmbeddedId
    private TopicUserId id;

    @MapsId("idTopic")
    @ManyToOne
    private TopicEntity topic;

    @MapsId("idUser")
    @ManyToOne
    private UserEntity user;

    private LocalDate joined_at;

    public TopicUserEntity(){}

    public TopicUserEntity(TopicUserId id, LocalDate joined_at) {
        this.id = id;
        this.joined_at = joined_at;
    }

    public TopicEntity getTopic() {
        return topic;
    }

    public void setTopic(TopicEntity topic) {
        this.topic = topic;
    }

    public UserEntity getUser() {
        return user;
    }

    public void setUser(UserEntity user) {
        this.user = user;
    }

    public LocalDate getJoined_at() {
        return joined_at;
    }

    public void setJoined_at(LocalDate joined_at) {
        this.joined_at = joined_at;
    }

    public TopicUserId getId() {
        return id;
    }
}
